#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
CONTENT_SOURCE="${1:-}"

if [[ -z "${CONTENT_SOURCE}" ]]; then
  case "$(uname -s)" in
    Darwin)
      CONTENT_SOURCE="${HOME}/Library/Application Support/Steam/steamapps/common/Assault Wing"
      ;;
    Linux)
      CONTENT_SOURCE="${HOME}/.local/share/Steam/steamapps/common/Assault Wing"
      ;;
  esac
fi

if [[ -z "${CONTENT_SOURCE}" || ! -d "${CONTENT_SOURCE}/Content" || ! -d "${CONTENT_SOURCE}/CoreContent" ]]; then
  echo "Browser content source must contain Content/ and CoreContent/." >&2
  echo "Install Assault Wing from Steam or pass a desktop build directory:" >&2
  echo "  $0 /path/to/Assault\ Wing" >&2
  exit 1
fi

TARGET_ROOT="${REPO_ROOT}/AssaultWingBrowser/wwwroot"
TARGET_CONTENT="${TARGET_ROOT}/Content"
TARGET_CORE_CONTENT="${TARGET_ROOT}/CoreContent"

# These directories are generated and ignored by git. Replacing them prevents
# stale XNB files from surviving when the source content changes.
rm -rf "${TARGET_CONTENT}" "${TARGET_CORE_CONTENT}"
cp -R "${CONTENT_SOURCE}/Content" "${TARGET_CONTENT}"
cp -R "${CONTENT_SOURCE}/CoreContent" "${TARGET_CORE_CONTENT}"

# Browser audio uses the checked-in source WAV/MP3 files. The Steam build's
# processed DesktopGL audio XNBs are not compatible with a browser decoder.
cp "${REPO_ROOT}/AssaultWingCoreContent/sounds/"*.wav "${TARGET_CORE_CONTENT}/sounds/"
cp "${REPO_ROOT}/AssaultWingCoreContent/music/"*.mp3 "${TARGET_CORE_CONTENT}/music/"

(
  cd "${TARGET_ROOT}"
  find Content CoreContent -type f -print | LC_ALL=C sort
) > "${TARGET_ROOT}/content-manifest.txt"

FILE_COUNT="$(wc -l < "${TARGET_ROOT}/content-manifest.txt" | tr -d ' ')"
echo "Prepared ${FILE_COUNT} browser content files from ${CONTENT_SOURCE}"
