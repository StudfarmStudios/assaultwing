#!/usr/bin/env bash
# Publishes the AW2 browser build to its own R2 bucket and Worker.
# Safe to re-run: each deployment replaces the one current build in the bucket.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
WEB_DIR="${WEB_DIR:-${REPO_ROOT}/AssaultWingBrowser/bin/Release/net9.0/publish/wwwroot}"
BUCKET="${BUCKET:-aw2-web}"
CLOUDFLARE_ACCOUNT_ID="${CLOUDFLARE_ACCOUNT_ID:-abcf9ef5bc3bdc22974818e2e0b79d4e}"
WRANGLER_VERSION="${WRANGLER_VERSION:-4.118.0}"
WRANGLER_LOG_PATH="${WRANGLER_LOG_PATH:-${TMPDIR:-/tmp}/aw2-wrangler-logs}"
mkdir -p "${WRANGLER_LOG_PATH}"
export WRANGLER_LOG_PATH

if [[ -z "${CLOUDFLARE_API_TOKEN:-}" && -f "${SCRIPT_DIR}/.cf-token" ]]; then
  CLOUDFLARE_API_TOKEN="$(head -1 "${SCRIPT_DIR}/.cf-token" | tr -d '[:space:]')"
fi
export CLOUDFLARE_API_TOKEN

if [[ -n "${WRANGLER_BIN:-}" ]]; then
  WRANGLER=("${WRANGLER_BIN}")
elif command -v wrangler >/dev/null 2>&1; then
  WRANGLER=(wrangler)
else
  WRANGLER=(npx --yes "wrangler@${WRANGLER_VERSION}")
fi
wr() { "${WRANGLER[@]}" "$@"; }

[[ -n "${CLOUDFLARE_API_TOKEN:-}" ]] || {
  echo "Set CLOUDFLARE_API_TOKEN or create ${SCRIPT_DIR}/.cf-token" >&2
  exit 1
}
[[ -f "${WEB_DIR}/index.html" ]] || {
  echo "Missing ${WEB_DIR}/index.html — run scripts/build_browser.sh first" >&2
  exit 1
}
[[ -f "${WEB_DIR}/_framework/blazor.boot.json" ]] || {
  echo "Missing ${WEB_DIR}/_framework/blazor.boot.json — run scripts/build_browser.sh first" >&2
  exit 1
}

export BUCKET WEB_DIR WRANGLER_VERSION CLOUDFLARE_ACCOUNT_ID

echo "== identity =="
wr whoami 2>&1 | grep -iE 'account name|not authenticated' | head -2

echo "== bucket =="
if wr r2 bucket list 2>/dev/null | grep -qE "^name: +${BUCKET}$"; then
  echo "  ${BUCKET} exists"
else
  wr r2 bucket create "${BUCKET}"
fi

echo "== assets =="
echo "  removing previous AW2 deployment"
"${SCRIPT_DIR}/empty-bucket.sh"
"${SCRIPT_DIR}/upload.sh"

echo "== worker =="
cd "${SCRIPT_DIR}"
wr deploy

echo "Published https://play-aw2.assaultwing.com/"
