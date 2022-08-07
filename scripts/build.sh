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
    APP_SHORT="AW Game"
    STEAM_APP_ID=1971370
    ;;
  dedicated_server)
    echo "Building Assault Wing Dedicated Server"
    APP_SHORT="AW Server"
    STEAM_APP_ID=2103880
    ;;
  *)
    echo "Unknown app parameter '$APP'"
    exit 2
esac

case "$CONFIGURATION" in
  debug)
    echo "Debug build selected"
    DOTNET_CONFIGURATION="Debug"
    STEAM_DEPOT_CONFIGURATION_OFFSET=5
    ;;
  release)
    echo "Release build selected"
    DOTNET_CONFIGURATION="Release"
    STEAM_DEPOT_CONFIGURATION_OFFSET=2
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
    STEAM_BUILD_BINARY='steambuild/builder_linux/linux32/steamcmd'
    STEAM_PLATFORM='linux'
    STEAM_RELATIVE_REPO_ROOT="../../"
    BUILD_CONTENT=0
    STEAM_DEPOT_PLATFORM_OFFSET=1
    STEAM_API_LIB="linux64/libsteam_api.so"
    ;;
  windows_wsl2)
    echo "Windows drive mounted in WSL2 selected"
    STEAM_CMD='steambuild\builder\steamcmd.exe'
    STEAM_PLATFORM='windows'
    STEAM_RELATIVE_REPO_ROOT='..\..\'
    BUILD_CONTENT=1 # Content building only works on windows
    STEAM_DEPOT_PLATFORM_OFFSET=0
    STEAM_API_LIB="win64/steam_api64.dll"
    ;;
  mac)
    echo "Mac / OSX platform selected"
    STEAM_CMD='steambuild/builder_osx/steamcmd.sh'
    STEAM_BUILD_BINARY='steambuild/builder_osx/steamcmd'
    STEAM_PLATFORM='mac'
    STEAM_RELATIVE_REPO_ROOT="../../"
    BUILD_CONTENT=0
    STEAM_DEPOT_PLATFORM_OFFSET=2
    STEAM_API_LIB="osx/libsteam_api.dylib"
    ;;
  *)
    echo "Unknown platform parameter '$PLATFORM'"
    exit 2  
esac

if [[ -z "${STEAMWORKS_SDK_PATH:=}" ]]; then
  STEAMWORKS_SDK_AVAILABLE=0
else
  STEAMWORKS_SDK_AVAILABLE=1
fi

if (( $BUILD_STEAM )); then
  if [[ -z "${AW_STEAM_BUILD_ACCOUNT:=}" || -z "${AW_STEAM_BUILD_PASSWORD:=}" ]]; then
    echo "Environment variables AW_STEAM_BUILD_ACCOUNT and AW_STEAM_BUILD_PASSWORD must be defined"
    exit 2
  fi
  if (( ! STEAMWORKS_SDK_AVAILABLE )); then
    echo "Environment variable STEAMWORKS_SDK_PATH must be defined"
    exit 2
  fi
fi

if (( $STEAMWORKS_SDK_AVAILABLE )); then
  if [[ ! -e "${STEAMWORKS_SDK_PATH}/redistributable_bin" ]]; then
    echo "Can't find redistributable_bin folder under STEAMWORKS_SDK_PATH=$STEAMWORKS_SDK_PATH"
    exit 2
  fi
fi

if [[ $(git describe --tags) =~ v([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)$ ]]; then 
  ASSAULT_WING_VERSION="${BASH_REMATCH[1]}"; 
  echo "Version number found from Git tags: $ASSAULT_WING_VERSION"
else
  echo "No Git tag of the format v1.2.3.4 found for this commit. This is a developer build."
  ASSAULT_WING_VERSION=""
fi

echo "STEAM_CMD=$STEAM_CMD"
echo "PLATFORM=$PLATFORM"
echo "DOTNET_CONFIGURATION=$DOTNET_CONFIGURATION"
echo "MODE=$MODE"
echo "APP=$APP"
echo "AW_STEAM_BUILD_ACCOUNT=$AW_STEAM_BUILD_ACCOUNT"
echo "BUILD_CONTENT=$BUILD_CONTENT"
    
BUILD_DESCRIPTION="${APP_SHORT} ${ASSAULT_WING_VERSION:-DEV} ${STEAM_PLATFORM} ${CONFIGURATION}"
echo "BUILD_DESCRIPTION=${BUILD_DESCRIPTION}"

ROOT=.
SOLUTION="AssaultWing.sln"
DOTNET_PLATFORM="x64"

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


SERVER_BUILD_FOLDER="DedicatedServer/bin/x64/${DOTNET_CONFIGURATION}/netcoreapp6.0"
GAME_BUILD_FOLDER="AssaultWing/bin/x64/${DOTNET_CONFIGURATION}/netcoreapp6.0"

raw_clean() {
  echo Cleaning folders
  echo Remove "*/bin/*": */bin/*
  rm -rf */bin/*
}

copy_steam_files() {
  echo "Copy steam supporting files from ${STEAMWORKS_SDK_PATH}"
  rsync -ua "${STEAMWORKS_SDK_PATH}/tools/ContentBuilder/" steambuild/
  mkdir -p "${SERVER_BUILD_FOLDER}" "${GAME_BUILD_FOLDER}"
  cp -a "${STEAMWORKS_SDK_PATH}/redistributable_bin/${STEAM_API_LIB}" "${SERVER_BUILD_FOLDER}"
  cp -a "${STEAMWORKS_SDK_PATH}/redistributable_bin/${STEAM_API_LIB}" "${GAME_BUILD_FOLDER}"
  cp "scripts/steam_appid.txt" $SERVER_BUILD_FOLDER
  cp "scripts/steam_appid.txt" $GAME_BUILD_FOLDER
  if [[ "$PLATFORM" == 'mac' || "$PLATFORM" == 'linux' ]]; then
    # On unix like platforms, the Steam tools are not executable "out of the box"... fix that here
    chmod a+rx $STEAM_CMD $STEAM_BUILD_BINARY
  fi
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
  platform_run dotnet build $SOLUTION --nologo --verbosity=quiet "/p:AssaultWingVersion=$ASSAULT_WING_VERSION" "/p:Configuration=$DOTNET_CONFIGURATION" "/p:Platform=$DOTNET_PLATFORM"

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

if (( $STEAMWORKS_SDK_AVAILABLE )); then
  # Copy steam files if they are available. This helps development
  copy_steam_files  
fi

if (( $BUILD_STEAM )); then
  # Depot numbers follow a pattern. See docs/steam-deploy.md
  STEAM_DEPOT="$(( $STEAM_APP_ID + $STEAM_DEPOT_PLATFORM_OFFSET + $STEAM_DEPOT_CONFIGURATION_OFFSET ))"
  STEAM_BUILD_FILE="steam_build_${APP}_${STEAM_PLATFORM}_${CONFIGURATION}.vdf"
  STEAM_BUILD_FILE_PATH="${STEAM_RELATIVE_REPO_ROOT}scripts/output/${STEAM_BUILD_FILE}"

  if (( $BUILD_CONTENT )) && [[ $APP == "assault_wing" ]]; then
    CONTENT_STEAM_DEPOT_LINE='"1971371" "..\steam_build_content_files.vdf"'
  else
    CONTENT_STEAM_DEPOT_LINE=''
  fi

  mkdir -p scripts/output

  echo "Generating Steam build file scripts/output/${STEAM_BUILD_FILE}"

  cat > "scripts/output/${STEAM_BUILD_FILE}" <<EOF
"AppBuild"
{
    "AppID" "${STEAM_APP_ID}" // ${APP_SHORT} Steam AppID
    "Desc" "${BUILD_DESCRIPTION}"

    // The assaultwing source root directory relative to the scripts/output where
    // this file ${STEAM_BUILD_FILE} is generated to.
    "ContentRoot" "..\.."

    // Build output folder for build logs and build cache files. It is the same
    // folder where this generated script is in.
    "BuildOutput" "."

    "Depots"
    {
        ${CONTENT_STEAM_DEPOT_LINE}
        "${STEAM_DEPOT}" "..\steam_build_${APP}_${CONFIGURATION}_files.vdf"
    }
}
EOF

  echo "Uploading ${BUILD_DESCRIPTION} to Steam"
  platform_run "$STEAM_CMD" +login "$AW_STEAM_BUILD_ACCOUNT" "$AW_STEAM_BUILD_PASSWORD" +run_app_build $STEAM_BUILD_FILE_PATH +quit
fi

