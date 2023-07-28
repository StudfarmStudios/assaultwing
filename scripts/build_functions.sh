#!/bin/bash

if [ -z "$BASH" ] ;then echo "Please run this script $0 with bash"; exit 1; fi

set -euo pipefail
IFS=$'\n\t'

set_variables_by_app() {
  case "$APP" in
    assault_wing)
      APP_SHORT="AW Game"
      STEAM_APP_ID=1971370
      PROJECT_NAME=AssaultWing
      ;;
    dedicated_server)
      APP_SHORT="AW Server"
      STEAM_APP_ID=2103880
      PROJECT_NAME=DedicatedServer
      ;;
    *)
      echo "Unknown app value '$APP'"
      exit 2
  esac

  PROJECT_FILE="./${PROJECT_NAME}/${PROJECT_NAME}.csproj"
}

set_variables_by_configuration() {
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
}

set_variables_by_target_platform() {
  case "$TARGET_PLATFORM" in
    linux)
      STEAM_DEPOT_PLATFORM_OFFSET=1
      STEAM_API_LIB="linux64/libsteam_api.so"
      DOTNET_RUNTIME=linux-x64
      ;;
    windows)
      STEAM_DEPOT_PLATFORM_OFFSET=0
      STEAM_API_LIB="win64/steam_api64.dll"
      DOTNET_RUNTIME=win-x64
      ;;
    mac)
      STEAM_DEPOT_PLATFORM_OFFSET=2
      STEAM_API_LIB="osx/libsteam_api.dylib"
      DOTNET_RUNTIME=osx-x64
      ;;
    *)
      echo "Unknown platform value '$TARGET_PLATFORM'"
      exit 2  
  esac
}

set_variables_by_current_platform() {
  case "$CURRENT_PLATFORM" in
  linux)
      echo "Linux platform selected"	   
      STEAM_CMD='steambuild/builder_linux/steamcmd.sh'
      STEAM_BUILD_BINARY='steambuild/builder_linux/linux32/steamcmd'
      STEAM_RELATIVE_REPO_ROOT="../../"
      BUILD_CONTENT=0
      CURRENT_PLATFORM_AS_TARGET_PLATFORM=linux
      ;;
  windows_wsl2)
      echo "Windows drive mounted in WSL2 selected"
      STEAM_CMD='steambuild\builder\steamcmd.exe'
      STEAM_RELATIVE_REPO_ROOT='..\..\'
      BUILD_CONTENT=1 # Content building only works on windows
      CURRENT_PLATFORM_AS_TARGET_PLATFORM=windows
      ;;
  mac)
      echo "Mac / OSX platform selected"
      STEAM_CMD='steambuild/builder_osx/steamcmd.sh'
      STEAM_BUILD_BINARY='steambuild/builder_osx/steamcmd'
      STEAM_RELATIVE_REPO_ROOT="../../"
      BUILD_CONTENT=0
      CURRENT_PLATFORM_AS_TARGET_PLATFORM=mac
      ;;
  *)
      echo "Unknown current platform parameter '$CURRENT_PLATFORM'"
      exit 2  
  esac
}

steam_sdk_configuration() {
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
    if [[ ! -e steambuild ]]; then
      echo "Making initial copy of Steam Content builder to the steambuild/ folder"
      rsync -ua "${STEAMWORKS_SDK_PATH}/tools/ContentBuilder/" steambuild/
    fi
  fi

  if (( $STEAMWORKS_SDK_AVAILABLE )); then
    if [[ ! -e "${STEAMWORKS_SDK_PATH}/redistributable_bin" ]]; then
      echo "Can't find redistributable_bin folder under STEAMWORKS_SDK_PATH=$STEAMWORKS_SDK_PATH"
      exit 2
    fi
    if [[ "$CURRENT_PLATFORM" == 'mac' || "$CURRENT_PLATFORM" == 'linux' ]]; then
      # On unix like platforms, the Steam tools are not executable "out of the box"... fix that here
      chmod a+rx $STEAM_CMD $STEAM_BUILD_BINARY
    fi
  fi
}

set_assault_wing_version_variable() {
  if [[ $(git describe --tags) =~ v([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)$ ]]; then 
    ASSAULT_WING_VERSION="${BASH_REMATCH[1]}"; 
    echo "Version number found from Git tags: $ASSAULT_WING_VERSION"
  else
    echo "No Git tag of the format v1.2.3.4 found for this commit. This is a developer build."
    ASSAULT_WING_VERSION=""
  fi
}

set_common_variables() {
  set_variables_by_current_platform
  set_variables_by_configuration
  set_assault_wing_version_variable
  steam_sdk_configuration
  ROOT=.
  SOLUTION="AssaultWing.sln"
  DOTNET_PLATFORM="x64"
  DOTNET_FRAMEWORK=netcoreapp6.0
}

echo_common_variables() {
  echo "STEAM_CMD=$STEAM_CMD"
  echo "CURRENT_PLATFORM=$CURRENT_PLATFORM"
  echo "DOTNET_CONFIGURATION=$DOTNET_CONFIGURATION"
  echo "MODE=${MODE:-<DEFAULT>}"
  echo "AW_STEAM_BUILD_ACCOUNT=${AW_STEAM_BUILD_ACCOUNT:-<UNDEFINED>}"
  echo "BUILD_CONTENT=$BUILD_CONTENT"
  echo "TARGET_PLATFORMS_TO_BUILD=${TARGET_PLATFORMS_TO_BUILD[@]}"
  echo "APPS_TO_BUILD=${APPS_TO_BUILD[@]}"
}

set_app_variables() {
  STEAM_BUILD_FILE="steam_build_${CONFIGURATION}_${APP}.vdf"
  STEAM_BUILD_FILE_PATH="${STEAM_RELATIVE_REPO_ROOT}scripts/output/${STEAM_BUILD_FILE}"
  BUILD_DESCRIPTION="${APP_SHORT} ${ASSAULT_WING_VERSION:-DEV} ${TARGET_PLATFORM} ${CONFIGURATION}"
}

echo_app_variables() {
  echo "STEAM_BUILD_FILE_PATH=$STEAM_BUILD_FILE_PATH"
  echo "BUILD_DESCRIPTION=${BUILD_DESCRIPTION}"
}

set_build_variables() {
  set_variables_by_target_platform
  set_variables_by_app
  # Depot numbers follow a pattern. See docs/steam-deploy.md
  STEAM_DEPOT="$(( $STEAM_APP_ID + $STEAM_DEPOT_PLATFORM_OFFSET + $STEAM_DEPOT_CONFIGURATION_OFFSET ))"
  APP_BUILD_FOLDER="scripts/output/${APP}-${TARGET_PLATFORM}-${CONFIGURATION}"  
  STEAM_APP_DEPOT_FILE="${APP}-${TARGET_PLATFORM}-${CONFIGURATION}.vdf"
  set_app_variables
}

# For copying steam files to ide builds
set_ide_build_variables() {
  set_variables_by_target_platform
  set_variables_by_app
  STEAM_DEPOT=""
  APP_BUILD_FOLDER="${PROJECT_NAME}/bin/${DOTNET_PLATFORM}/${DOTNET_CONFIGURATION}/${DOTNET_FRAMEWORK}"
  STEAM_APP_DEPOT_FILE=""
  set_app_variables
}

echo_build_variables() {
  echo "TARGET_PLATFORM=$TARGET_PLATFORM"
  echo "DOTNET_CONFIGURATION=$DOTNET_CONFIGURATION"
  echo "APP=$APP"
  echo "STEAM_DEPOT=$STEAM_DEPOT"
  echo_app_variables
}

platform_run() {
  if [ "$CURRENT_PLATFORM" = "windows_wsl2" ]; then
    # bash with WSL runs build commands in some weird context. OTOH building on linux is probably fine.
    # TODO: Add logic here on when to do cmd /c
    echo cmd.exe /c "$@"
    cmd.exe /c "$@"
  else
    echo "$@"
    "$@"
  fi
}

copy_steam_files() {
  echo "Copy steam supporting files from ${STEAMWORKS_SDK_PATH} to ${APP_BUILD_FOLDER}"
  mkdir -p "${APP_BUILD_FOLDER}"
  cp -a "${STEAMWORKS_SDK_PATH}/redistributable_bin/${STEAM_API_LIB}" "${APP_BUILD_FOLDER}"
  cp "scripts/steam_appid.txt" "$APP_BUILD_FOLDER"
}

clean_build_setup() {
  echo dotnet restore
  platform_run dotnet restore $SOLUTION

  echo dotnet clean
  platform_run dotnet clean $SOLUTION --nologo --verbosity=quiet
}

publish_one_app_target() {  
  if (( $CLEAN )); then
    # A sanity check with the rm to avoid deleting home dir etc
    if [[ "$APP_BUILD_FOLDER" =~ ^scripts/output/.* ]]; then
      echo "Deleting $APP_BUILD_FOLDER"
      rm -rf "$APP_BUILD_FOLDER"
    fi
  fi

  echo "Building ${BUILD_DESCRIPTION} to ${APP_BUILD_FOLDER}"

  platform_run dotnet publish "$PROJECT_FILE" \
    --nologo --verbosity=quiet \
    "/p:Configuration=$DOTNET_CONFIGURATION" \
    "/p:AssaultWingVersion=$ASSAULT_WING_VERSION" \
    --self-contained \
    --framework "$DOTNET_FRAMEWORK" \
    --runtime "$DOTNET_RUNTIME" \
    --output "$APP_BUILD_FOLDER"

  if (( $STEAMWORKS_SDK_AVAILABLE )); then
    copy_steam_files
  fi
  
  echo Built $APP_BUILD_FOLDER
}

publish() {
  if (( $CLEAN )); then
    clean_build_setup
  fi

  for TARGET_PLATFORM in ${TARGET_PLATFORMS_TO_BUILD[*]}; do
    for APP in ${APPS_TO_BUILD[*]}; do
      set_build_variables
      echo_build_variables
      publish_one_app_target
    done
  done
}

build() {
  if (( $CLEAN )); then
    clean_build_setup
  fi

  # It seems we need to define build profiles to build for Linux & Mac here. https://github.com/dotnet/sdk/issues/14281#issuecomment-863499124
  # /target:Publish
  echo dotnet build
  platform_run dotnet build $SOLUTION \
    --nologo --verbosity=quiet \
    "/p:AssaultWingVersion=$ASSAULT_WING_VERSION"  \
    "/p:Configuration=$DOTNET_CONFIGURATION" \
    "/p:Platform=$DOTNET_PLATFORM"

  if (( $STEAMWORKS_SDK_AVAILABLE )); then
    for APP_BUILD_FOLDER in ${EXECUTABLE_APP_FOLDERS[*]}; do
      # Copy steam files if they are available. This helps development
      copy_steam_files
    done
  fi

  echo Built: */bin/*
}

generate_steam_app_depot_file() {
  mkdir -p scripts/output

  echo "Generating the app Steam depot file scripts/output/${STEAM_APP_DEPOT_FILE}"

  if [[ $APP == "dedicated_server" ]]; then
    # Dedicated server needs the app id file, but the game should not have it 
    EXCLUDE_APP_ID_LINE=''
  else
    EXCLUDE_APP_ID_LINE='"FileExclusion" "${APP_BUILD_FOLDER}\steam_appid.txt"'    
  fi  

  cat > "scripts/output/${STEAM_APP_DEPOT_FILE}" <<EOF
"DepotBuild"
{
    // ${BUILD_DESCRIPTION}
    "FileMapping"
    {
        "LocalPath" "${APP_BUILD_FOLDER}\*"
        "DepotPath" "." // mapped into the root of the depot
        "recursive" "1" // include all subfolders
    }
    ${EXCLUDE_APP_ID_LINE}
    "FileExclusion" "${APP_BUILD_FOLDER}\Content*"
    "FileExclusion" "${APP_BUILD_FOLDER}\CoreContent*"
    "FileExclusion" "${APP_BUILD_FOLDER}\Log*.txt" // Sometimes these can be left over if this is not 1 100% clean build
    "FileExclusion" "${APP_BUILD_FOLDER}\AssaultWing_config.xml" // Sometimes this can be left over if this is not 1 100% clean build
    "FileExclusion" "${APP_BUILD_FOLDER}\*.pdb" // No debug files
}
EOF
}

generate_steam_build_file_for_app() {
  mkdir -p scripts/output

  STEAM_APP_DEPOT_LINES=""
  for TARGET_PLATFORM in ${TARGET_PLATFORMS_TO_BUILD[*]}; do
    set_build_variables
    generate_steam_app_depot_file
    STEAM_APP_DEPOT_LINE="\"${STEAM_DEPOT}\" \"${STEAM_APP_DEPOT_FILE}\""
    printf -v STEAM_APP_DEPOT_LINES "${STEAM_APP_DEPOT_LINES}\n${STEAM_APP_DEPOT_LINE}"
  done

  echo "Generating the Steam build file scripts/output/${STEAM_BUILD_FILE}"

  if (( $BUILD_CONTENT )) && [[ $APP == "assault_wing" ]]; then
    CONTENT_STEAM_DEPOT_LINE='"1971371" "..\steam_build_content_files.vdf"'
    CONTENT_DESC_PART="content:yes"
  else
    CONTENT_STEAM_DEPOT_LINE=''
    CONTENT_DESC_PART="content:no"
  fi  

  cat > "scripts/output/${STEAM_BUILD_FILE}" <<EOF
"AppBuild"
{
    "AppID" "${STEAM_APP_ID}" // ${APP_SHORT} Steam AppID
    "Desc" "${APP} ${ASSAULT_WING_VERSION:-DEV} platforms:${TARGET_PLATFORMS_TO_BUILD[@]} ${CONTENT_DESC_PART} config:${CONFIGURATION}"

    // The assaultwing source root directory relative to the scripts/output where
    // this file ${STEAM_BUILD_FILE} is generated to.
    "ContentRoot" "..\.."

    // Build output folder for build logs and build cache files. It is the same
    // folder where this generated script is in.
    "BuildOutput" "."

    "Depots"
    {
        ${CONTENT_STEAM_DEPOT_LINE}
        ${STEAM_APP_DEPOT_LINES}  
    }
}
EOF
}

generate_steam_build_files() {
  for APP in ${APPS_TO_BUILD[*]}; do
    set_app_variables
    echo_app_variables
    generate_steam_build_file_for_app
  done
}

steam_depot_upload() {
  for APP in ${APPS_TO_BUILD[*]}; do
    set_app_variables
    echo_app_variables
    echo "Uploading ${BUILD_DESCRIPTION} to Steam"
    platform_run "$STEAM_CMD" +login "$AW_STEAM_BUILD_ACCOUNT" "$AW_STEAM_BUILD_PASSWORD" +run_app_build $STEAM_BUILD_FILE_PATH +quit
  done
}
