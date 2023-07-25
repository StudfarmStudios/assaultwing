#!/bin/bash

if [ -z "$BASH" ] ;then echo "Please run this script $0 with bash"; exit 1; fi

set -euo pipefail
IFS=$'\n\t'

if [ $# -ne 1 ]; then
  echo "This is a bash script to copy steam files to ide build folders"
  echo "Usage: $0 [Current Platform]"
  echo "Current Platform:"
  echo "  'windows_wsl2' = Windows drive mounted on WSL2."
  echo "  'linux'        = Unbuntu 20.04 or similar"
  echo "  'mac'          = Mac OSx"
  exit 2
fi

CURRENT_PLATFORM=$1

BUILD_STEAM=0
IFS=$' ' APPS_TO_BUILD=(assault_wing dedicated_server)
IFS=$' ' CONFIGURATIONS=(release debug)

. scripts/build_functions.sh


for CONFIGURATION in ${CONFIGURATIONS[*]}; do
  set_common_variables
  TARGET_PLATFORM="$CURRENT_PLATFORM_AS_TARGET_PLATFORM"
  for APP in ${APPS_TO_BUILD[*]}; do
    set_ide_build_variables
    copy_steam_files
  done
done

echo "Trying to copy content files from installed steam game to ide build folders."

# TODO: This currently only has a chance of working on Mac
STEAM_GAME_INSTALL_FOLDER="/Users/${USER}/Library/Application Support/Steam/steamapps/common/Assault Wing"

for CONFIGURATION in ${CONFIGURATIONS[*]}; do
  set_common_variables
  TARGET_PLATFORM="$CURRENT_PLATFORM_AS_TARGET_PLATFORM"
  for APP in ${APPS_TO_BUILD[*]}; do
    set_ide_build_variables
    echo "Copying from '${STEAM_GAME_INSTALL_FOLDER}' to '${APP_BUILD_FOLDER}'"
    rsync -a "${STEAM_GAME_INSTALL_FOLDER}/Content/" "$APP_BUILD_FOLDER/Content/"
    rsync -a "${STEAM_GAME_INSTALL_FOLDER}/CoreContent/" "$APP_BUILD_FOLDER/CoreContent/"
  done
done
