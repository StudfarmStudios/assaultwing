#!/usr/bin/env bash
# Uploads every logical web-publish file to the AW2 bucket. Generated .br files
# become the stored body for their unsuffixed URL; .gz twins are not uploaded
# because all browsers capable of running this build support Brotli.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUCKET="${BUCKET:-aw2-web}"
WEB_DIR="${WEB_DIR:?deploy.sh sets WEB_DIR}"
UPLOAD_JOBS="${UPLOAD_JOBS:-4}"
WRANGLER_VERSION="${WRANGLER_VERSION:-4.118.0}"
WRANGLER_LOG_PATH="${WRANGLER_LOG_PATH:-${TMPDIR:-/tmp}/aw2-wrangler-logs}"
mkdir -p "${WRANGLER_LOG_PATH}"
export WRANGLER_LOG_PATH

if [[ -n "${WRANGLER_BIN:-}" && -x "${WRANGLER_BIN}" ]]; then
  WRANGLER=("${WRANGLER_BIN}")
elif command -v wrangler >/dev/null 2>&1; then
  WRANGLER=(wrangler)
else
  WRANGLER=(npx --yes "wrangler@${WRANGLER_VERSION}")
fi
wr() { "${WRANGLER[@]}" "$@"; }

content_type() {
  case "${1##*.}" in
    html) echo 'text/html; charset=utf-8' ;;
    css) echo 'text/css; charset=utf-8' ;;
    js|mjs) echo 'text/javascript; charset=utf-8' ;;
    json) echo 'application/json; charset=utf-8' ;;
    xml) echo 'application/xml; charset=utf-8' ;;
    txt) echo 'text/plain; charset=utf-8' ;;
    wasm) echo 'application/wasm' ;;
    png) echo 'image/png' ;;
    wav) echo 'audio/wav' ;;
    mp3) echo 'audio/mpeg' ;;
    wmv) echo 'video/x-ms-wmv' ;;
    *) echo 'application/octet-stream' ;;
  esac
}

put_one() {
  local logical="$1" source="${WEB_DIR}/$1" encoding=''
  if [[ -f "${source}.br" ]]; then
    source="${source}.br"
    encoding='br'
  fi
  local args=(r2 object put "${BUCKET}/${logical}"
              --file "${source}" --content-type "$(content_type "${logical}")" --remote)
  [[ -n "${encoding}" ]] && args+=(--content-encoding "${encoding}")
  printf '  %-72s %8s\n' "${logical}" "$(du -h "${source}" | cut -f1)"
  local attempt error_log="${TMPDIR:-/tmp}/aw2-upload-$$-${RANDOM}.log"
  for attempt in 1 2 3 4; do
    if wr "${args[@]}" > /dev/null 2> "${error_log}"; then
      rm -f "${error_log}"
      return 0
    fi
    echo "    retry ${attempt}/4: ${logical}" >&2
    sleep "$((attempt * 2))"
  done
  cat "${error_log}" >&2
  rm -f "${error_log}"
  return 1
}

PIDS=()
FAILED=0
wait_batch() {
  local pid
  for pid in "${PIDS[@]}"; do
    wait "${pid}" || FAILED=1
  done
  PIDS=()
  if (( FAILED != 0 )); then
    echo "At least one R2 object upload failed" >&2
    exit 1
  fi
}

while IFS= read -r logical; do
  put_one "${logical}" &
  PIDS+=("$!")
  (( ${#PIDS[@]} < UPLOAD_JOBS )) || wait_batch
done < <(
  cd "${WEB_DIR}"
  find . -type f ! -name '*.br' ! -name '*.gz' -print |
    sed 's#^\./##' |
    LC_ALL=C sort
)
(( ${#PIDS[@]} == 0 )) || wait_batch

echo "  upload complete"
