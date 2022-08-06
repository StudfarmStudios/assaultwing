#!/bin/bash

if [ -z "$BASH" ] ;then echo "Please run this script $0 with bash"; exit 1; fi

set -euo pipefail
IFS=$'\n\t'

if [ $# -lt 3 ]; then
  echo "This is a bash script build Assault Wing and upload it to Steam."
  echo "Usage: $0 [Platform] [App] [Configuration] [Mode]"
  echo "Platform:"
  echo "  'windows_wsl2' = Windows drive mounted on WSL2."
  echo "  'linux'        = Unbuntu 20.04 or similar"
  echo "  'mac'          = Mac TBD"
  echo "App:"
  echo "  'assault_wing'     = The main game pacakge"
  echo "  'dedicated_server' = Dedicated server package"
  echo "Configuration:"
  echo "  'release' = for actual release quality build"
  echo "  'debug'   = for a developer build."
  echo "Mode:"
  echo "  'skip_build'           = go directly to the steam release (USE WITH CARE! YOU MAY PUBLISH BROKEN / WRONG DEPOTS!)."
  echo "  'skip_clean'           = unclean build to speed up things"
  echo "  'skip_clean_and_steam' = Skip clean build and the steam upload. For a fast sanity check"
  exit 2
elif [ $# -gt 4 ]; then
  echo 1>&2 "$0: too many arguments"
  exit 2
fi

PLATFORM=$1
APP=$2
CONFIGURATION=$3
MODE=${4:-}

case "$APP" in
  assault_wing)
    echo "Building Assault Wing"
    ;;
  dedicated_server)
    echo "Building Assault Wing Dedicated Server"
    ;;
  *)
    echo "Unknown app parameter '$APP'"
    exit 2
esac

case "$CONFIGURATION" in
  debug)
    echo "Debug build selected"
    DOTNET_CONFIGURATION="Debug"
    ;;
  release)
    echo "Release build selected"
    DOTNET_CONFIGURATION="Release"
    ;;
  *)
    echo "Unknown configuration '$CONFIGURATION'"
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


case "$PLATFORM" in
  linux)
    echo "Linux platform selected"	   
    STEAM_CMD='steambuild/builder_linux/steamcmd.sh'
    if (( $BUILD_STEAM )); then
      # The linux tools are not executable "out of the box"... fix that here
      chmod -c a+rx $STEAM_CMD
      chmod -c a+rx 'steambuild/builder_linux/linux32/steamcmd'
    fi
    STEAM_PLATFORM='linux'
    STEAM_SCRIPT_FOLDER_RELATIVE="../../scripts/"
    DISABLE_CONTENT_BUILDING=true
    ;;
  windows_wsl2)
    echo "Windows drive mounted in WSL2 selected"
    STEAM_CMD='steambuild\builder\steamcmd.exe'
    STEAM_PLATFORM='windows'
    STEAM_SCRIPT_FOLDER_RELATIVE='..\..\scripts\'
      DISABLE_CONTENT_BUILDING=false # Content building only works on windows
    ;;
  mac)
    echo "Mac builds TBD"
    STEAM_CMD='steambuild/builder_mac/steamcmd.sh'
    if (( $BUILD_STEAM )); then
      # TODO: Test mac build with steam
      chmod a+rx $STEAM_CMD
      chmod a+rx 'steambuild/builder_linux/linux32/steamcmd'
    fi
    STEAM_PLATFORM='mac'
    STEAM_SCRIPT_FOLDER_RELATIVE="../../scripts/"
    DISABLE_CONTENT_BUILDING=true
    # exit 2
    ;;
  *)
    echo "Unknown platform parameter '$PLATFORM'"
    exit 2  
esac

if (( $BUILD_STEAM )); then
  if [[ -z "${AW_STEAM_BUILD_ACCOUNT:=}" || -z "${AW_STEAM_BUILD_PASSWORD:=}" ]]; then
    echo "Environment variables AW_STEAM_BUILD_ACCOUNT and AW_STEAM_BUILD_PASSWORD must be defined"
    exit 2
  fi
fi

echo "STEAM_CMD=$STEAM_CMD"
echo "PLATFORM=$PLATFORM"
echo "DOTNET_CONFIGURATION=$DOTNET_CONFIGURATION"
echo "MODE=$MODE"
echo "APP=$APP"
echo "AW_STEAM_BUILD_ACCOUNT=$AW_STEAM_BUILD_ACCOUNT"
echo "DISABLE_CONTENT_BUILDING=$DISABLE_CONTENT_BUILDING"
    
#ROOT=$(pwd)
#SOLUTION="${ROOT}\AssaultWing.sln"
ROOT=.
SOLUTION="AssaultWing.sln"
STEAM_BUILD_FILE="steam_build_${APP}_${STEAM_PLATFORM}_${CONFIGURATION}.vdf"
echo "STEAM_BUILD_FILE=$STEAM_BUILD_FILE"

if [[ ! -f "scripts/$STEAM_BUILD_FILE" ]]; then
  echo "Steam build file 'scripts/$STEAM_BUILD_FILE' does not exist."
  exit 2
fi

platform_run() {
  if [ "$PLATFORM" = "windows_wsl2" ]; then
    # bash with WSL runs build commands in some weird context. OTOH building on linux is probably fine.
    # TODO: Add logic here on when to do cmd /c
    echo cmd.exe /c "$@"
    cmd.exe /c "$@"
  else
    echo "$@"
    "$@"
  fi
}

raw_clean() {
  echo Cleaning folders
  echo Remove "*/bin/*": */bin/*
  rm -rf */bin/*
}

clean_build_setup() {
  echo dotnet restore
  platform_run dotnet restore $SOLUTION

  echo dotnet clean
  platform_run dotnet clean $SOLUTION --nologo --verbosity=quiet
}

build() {
  # It seems we need to define build profiles to build for Linux & Mac here. https://github.com/dotnet/sdk/issues/14281#issuecomment-863499124
  # /target:Publish
  echo dotnet build
  platform_run dotnet build $SOLUTION --nologo --verbosity=quiet "/p:Configuration=$DOTNET_CONFIGURATION" "/p:Platform=Any Cpu"

  echo Built: */bin/*
}

if (( $BUILD )); then
  echo "Configuration=${CONFIGURATION}"
  if (( $CLEAN )); then
    raw_clean
    clean_build_setup
  fi  
  build
else 
  echo "Skipping cleaning and building due to '$MODE'"
fi

if (( $BUILD_STEAM )); then
  platform_run "$STEAM_CMD" +login "$AW_STEAM_BUILD_ACCOUNT" "$AW_STEAM_BUILD_PASSWORD" +run_app_build "${STEAM_SCRIPT_FOLDER_RELATIVE}${STEAM_BUILD_FILE}" +quit
fi

