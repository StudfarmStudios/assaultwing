#!/usr/bin/env bash
# Permanently removes the current AW2 web objects before a replacement upload.
set -euo pipefail

BUCKET="${BUCKET:-aw2-web}"
ACCOUNT_ID="${CLOUDFLARE_ACCOUNT_ID:?deploy.sh sets CLOUDFLARE_ACCOUNT_ID}"
DELETE_JOBS="${DELETE_JOBS:-8}"
API_ROOT="https://api.cloudflare.com/client/v4/accounts/${ACCOUNT_ID}/r2/buckets/${BUCKET}/objects"

command -v curl >/dev/null 2>&1 || { echo "curl is required" >&2; exit 1; }
command -v jq >/dev/null 2>&1 || { echo "jq is required" >&2; exit 1; }
[[ -n "${CLOUDFLARE_API_TOKEN:-}" ]] || { echo "CLOUDFLARE_API_TOKEN is required" >&2; exit 1; }

delete_one() {
  local key="$1" encoded response
  encoded="$(jq -rn --arg key "${key}" '$key | @uri')"
  response="$(curl --silent --show-error --fail-with-body --retry 3 \
    --request DELETE "${API_ROOT}/${encoded}" \
    --header "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}")"
  jq -e '.success == true' >/dev/null <<< "${response}"
}
export -f delete_one
export API_ROOT CLOUDFLARE_API_TOKEN

deleted=0
while true; do
  response="$(curl --silent --show-error --fail-with-body --retry 3 \
    "${API_ROOT}?per_page=1000" \
    --header "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}")"
  jq -e '.success == true' >/dev/null <<< "${response}"
  count="$(jq '.result | length' <<< "${response}")"
  (( count > 0 )) || break

  jq -j '.result[] | .key, "\u0000"' <<< "${response}" |
    xargs -0 -n 1 -P "${DELETE_JOBS}" bash -c 'delete_one "$1"' _
  deleted="$((deleted + count))"
  echo "  removed ${deleted} objects"
done

echo "  bucket empty (${deleted} objects removed)"
