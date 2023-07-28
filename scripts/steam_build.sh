#!/bin/bash

if [ -z "$BASH" ] ;then echo "Please run this script $0 with bash"; exit 1; fi

set -euo pipefail
IFS=$'\n\t'

if [ $# -lt 4 ]; then
  echo "This is a bash script to build Assault Wing and upload it to Steam."
  echo "Usage: $0 [Current Platform] [Target Platform] [App] [Configuration] [Mode]"
  echo "Current Platform:"
  echo "  'windows_wsl2' = Windows drive mounted on WSL2."
  echo "  'linux'        = Unbuntu 20.04 or similar"
  echo "  'mac'          = Mac OSx"
  echo "Target Platform:"
  echo "  'all'          = Build for all 3 platforms"
  echo "  'windows'      = Windows"
  echo "  'linux'        = Linux"
  echo "  'mac'          = Mac"
  echo "App:"
  echo "  'all'              = Build both"
  echo "  'assault_wing'     = The main game pacakge"
  echo "  'dedicated_server' = Dedicated server package"
  echo "Configuration:"
  echo "  'release' = for actual release quality build"
  echo "  'debug'   = for a developer build."
  echo "Mode:"
  echo "  'skip_build'           = go directly to the steam release (USE WITH CARE! YOU MAY PUBLISH BROKEN / WRONG DEPOTS!)."
  echo "  'skip_clean'           = unclean build to speed up things"
  echo "  'skip_clean_and_steam' = Skip clean build and the steam upload. For a fast sanity check"
  echo "  'skip_steam'           = Skip the steam build"
  exit 2
elif [ $# -gt 5 ]; then
  echo 1>&2 "$0: too many arguments"
  exit 2
fi

CURRENT_PLATFORM=$1
TARGET_PLATFORMS_TO_BUILD=$2
APPS_TO_BUILD=$3
CONFIGURATION=$4
MODE=${5:-}

case "$APPS_TO_BUILD" in
  all)
    echo "Building Assault Wing and the Dedicated Server"
    IFS=$' ' APPS_TO_BUILD=(assault_wing dedicated_server)
    ;;
  assault_wing)
    echo "Building Assault Wing"
    ;;
  dedicated_server)
    echo "Building Assault Wing Dedicated Server"
    ;;
  *)
    echo "Unknown apps to build parameter '$APPS_TO_BUILD'"
    exit 2
esac

case "$TARGET_PLATFORMS_TO_BUILD" in
  all)
    echo "Building for Windows, Linux and Mac"
    IFS=$' ' TARGET_PLATFORMS_TO_BUILD=(windows linux mac)
    ;;
  windows)
    echo "Building for Windows only"
    ;;
  linux)
    echo "Building for Linux only"
    ;;
  mac)
    echo "Building for Mac only"
    ;;
  *)
    echo "Unknown platforms to build for parameter '$TARGET_PLATFORMS_TO_BUILD'"
    exit 2
esac


case "$MODE" in
  skip_build)
    echo "skip_build: Only doing the Steam upload part!"
    CLEAN=0
    BUILD=0
    BUILD_STEAM=1
    ;;
  skip_clean)
    echo "skip_clean: Skipping clean. Build may not be 100% repeatable"
    CLEAN=0
    BUILD=1
    BUILD_STEAM=1
    ;;
  skip_clean_and_steam)
    echo "skip_clean_and_steam: Skipping both clean build and the steam upload."
    CLEAN=0
    BUILD=1
    BUILD_STEAM=0
    ;;
  skip_steam)
    echo "skip_steam: Skipping steam upload."
    CLEAN=1
    BUILD=1
    BUILD_STEAM=0
    ;;
  "")
    echo "No special mode"
    CLEAN=1
    BUILD=1
    BUILD_STEAM=1
    ;;
  *)
    echo "Unknown mode '$MODE'"
    exit 2  
esac

. scripts/build_functions.sh

set_common_variables
echo_common_variables

if (( $BUILD )); then
  publish
else 
  echo "Skipping cleaning and building due to '$MODE'"
fi

generate_steam_build_files

if (( $BUILD_STEAM )); then
  steam_depot_upload
fi


