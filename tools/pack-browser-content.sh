#!/usr/bin/env bash
# Replaces the browser build's hundreds of loose game files with a handful of
# lazy ZIP packs. The logical paths in content-manifest.txt stay unchanged, so
# the original game content API does not need to know how files are packaged.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
CONTENT_ROOT="${1:-${REPO_ROOT}/AssaultWingBrowser/wwwroot}"
PACK_ROOT="${CONTENT_ROOT}/packs"
PACK_MAP="${CONTENT_ROOT}/content-packs.tsv"
CONTENT_MANIFEST="${CONTENT_ROOT}/content-manifest.txt"
ZIP_BIN="${ZIP_BIN:-zip}"

command -v "${ZIP_BIN}" >/dev/null 2>&1 || {
  echo "The zip command is required to build browser content packs." >&2
  exit 1
}
[[ -d "${CONTENT_ROOT}/Content" && -d "${CONTENT_ROOT}/CoreContent" ]] || {
  echo "Browser content is missing under ${CONTENT_ROOT}; prepare it first." >&2
  exit 1
}

SOURCE_COUNT="$(find "${CONTENT_ROOT}/Content" "${CONTENT_ROOT}/CoreContent" -type f | wc -l | tr -d ' ')"
(( SOURCE_COUNT > 100 )) || {
  echo "Only ${SOURCE_COUNT} source content files were found; prepare fresh browser content before packing." >&2
  exit 1
}

TEMP_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/aw2-content-packs.XXXXXX")"
trap 'rm -rf "${TEMP_ROOT}"' EXIT
rm -rf "${PACK_ROOT}"
mkdir -p "${PACK_ROOT}"
: > "${PACK_MAP}"

(
  cd "${CONTENT_ROOT}"
  find Content CoreContent -type f -print | LC_ALL=C sort
) > "${CONTENT_MANIFEST}"

add_to_pack() {
  local pack="$1" logical="$2"
  printf '%s\n' "${logical}" >> "${TEMP_ROOT}/${pack}.list"
}

while IFS= read -r logical; do
  case "${logical}" in
    CoreContent/music/*.mp3)
      # HTMLMediaElement streams these URLs directly, so they stay loose and
      # are fetched one track at a time.
      ;;
    Content/models/*)
      add_to_pack graphics "${logical}"
      ;;
    CoreContent/textures/*)
      add_to_pack textures "${logical}"
      ;;
    CoreContent/fonts/*|CoreContent/effects/*)
      add_to_pack graphics "${logical}"
      ;;
    CoreContent/sounds/*)
      add_to_pack sounds "${logical}"
      ;;
    CoreContent/arenas/*.bin)
      arena_name="$(basename "${logical}" .bin | tr '[:upper:] ' '[:lower:]-' | tr -cd '[:alnum:]-_')"
      add_to_pack "arena-${arena_name}" "${logical}"
      ;;
    CoreContent/music/*)
      add_to_pack music "${logical}"
      ;;
    CoreContent/video/*)
      add_to_pack video "${logical}"
      ;;
    *)
      add_to_pack definitions "${logical}"
      ;;
  esac
done < "${CONTENT_MANIFEST}"

build_pack() {
  local list="$1" pack_name
  pack_name="$(basename "${list}" .list)"
  local pack_path="packs/${pack_name}.zip"
  (
    cd "${CONTENT_ROOT}"
    "${ZIP_BIN}" -9 -q "${pack_path}" -@ < "${list}"
  )
  while IFS= read -r logical; do
    printf '%s\t%s\n' "${logical}" "${pack_path}" >> "${PACK_MAP}"
    rm "${CONTENT_ROOT}/${logical}"
  done < "${list}"
  printf '  %-36s %8s (%s files)\n' "${pack_path}" \
    "$(du -h "${CONTENT_ROOT}/${pack_path}" | cut -f1)" \
    "$(wc -l < "${list}" | tr -d ' ')"
}

echo "Browser content packs:"
while IFS= read -r list; do
  build_pack "${list}"
done < <(find "${TEMP_ROOT}" -type f -name '*.list' -print | LC_ALL=C sort)

find "${CONTENT_ROOT}/Content" "${CONTENT_ROOT}/CoreContent" -type d -empty -delete

PACKED_COUNT="$(wc -l < "${PACK_MAP}" | tr -d ' ')"
LOOSE_COUNT="$((SOURCE_COUNT - PACKED_COUNT))"
echo "Packed ${PACKED_COUNT} files; left ${LOOSE_COUNT} streaming files loose."
