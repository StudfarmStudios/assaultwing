#!/bin/bash

if [ -z "$BASH" ] ;then echo "Please run this script $0 with bash"; exit 1; fi

set -euo pipefail
IFS=$'\n\t'

if [ $# -lt 2 ]; then
  echo "This is a bash script build Assault Wing inside the project folders."
  echo "It mainly helps by also copying the Steam related files."
  echo "Usage: $0 [Current Platform] [Configuration]"
  echo "Current Platform:"
  echo "  'windows_wsl2' = Windows drive mounted on WSL2."
  echo "  'linux'        = Unbuntu 20.04 or similar"
  echo "  'mac'          = Mac OSx"
  echo "Configuration:"
  echo "  'release'      = for actual release quality build"
  echo "  'debug'        = for a developer build."
  echo "Mode:"
  echo "  'clean'        = Delete everything to test repeatability"
  exit 2
elif [ $# -gt 3  ]; then
  echo 1>&2 "$0: too many arguments"
  exit 2
fi

MODE=${3:-}
case "$MODE" in
  clean)
    echo "Clean build selected"
    CLEAN=1
    ;;
  "")
    echo "No special mode"
    CLEAN=0
    ;;
  *)
    echo "Unknown mode '$MODE'"
    exit 2  
    ;;
esac

CURRENT_PLATFORM=$1
TARGET_PLATFORM=$CURRENT_PLATFORM
TARGET_PLATFORMS_TO_BUILD=$CURRENT_PLATFORM
IFS=$' ' APPS_TO_BUILD=(assault_wing dedicated_server)
CONFIGURATION=$2
BUILD_STEAM=0
BUILD=1

. scripts/build_functions.sh

set_common_variables

EXECUTABLE_APP_FOLDERS=("DedicatedServer/bin/x64/${DOTNET_CONFIGURATION}/${DOTNET_FRAMEWORK}" "AssaultWing/bin/x64/${DOTNET_CONFIGURATION}/${DOTNET_FRAMEWORK}")

set_variables_by_target_platform
echo_common_variables
build

echo
echo "Next run one of the following:"
for APP_BUILD_FOLDER in ${EXECUTABLE_APP_FOLDERS[*]}; do
  echo "  cd $APP_BUILD_FOLDER"
done

echo
echo "And then run ./AssaultWing or ./DedicatedServer"