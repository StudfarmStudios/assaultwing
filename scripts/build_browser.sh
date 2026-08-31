#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
CONTENT_SOURCE="${1:-}"
DOTNET_CMD="${AW_DOTNET:-dotnet}"
PUBLISH_ROOT="${REPO_ROOT}/AssaultWingBrowser/bin/Release/net9.0/publish"

cd "${REPO_ROOT}"

case "$(uname -s)" in
  Darwin|Linux)
    "${REPO_ROOT}/tools/prepare-browser-content.sh" "${CONTENT_SOURCE}"
    "${REPO_ROOT}/tools/pack-browser-content.sh"
    BUILD_CONTENT=false
    ;;
  *)
    # On Windows, KNI compiles BlazorGL XNBs directly from the checked-in
    # MGCB projects. No Steam installation is required.
    BUILD_CONTENT=true
    ;;
esac

# Fingerprinted WebAssembly files change names between publishes. Recreate only
# the generated browser publish directory so stale runtimes aren't deployed.
rm -rf "${PUBLISH_ROOT}"

"${DOTNET_CMD}" publish AssaultWingBrowser/AssaultWingBrowser.csproj \
  --configuration Release \
  -p:BuildBrowserContent="${BUILD_CONTENT}"

echo "Browser build: ${PUBLISH_ROOT}/wwwroot"
