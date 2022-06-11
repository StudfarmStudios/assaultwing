#!/bin/bash

if [ -z "$BASH" ] ;then echo "Please run this script $0 with bash"; exit 1; fi

set -euo pipefail
IFS=$'\n\t'

if [ $# -lt 1 ]; then
  echo "This is a bash script built to work on plain Linux (TBD) OR Windows drive mounted on WSL2."
  echo "Usage: $0 [Configuration]"
  echo "Configuration: 'Release' for release. If 'Debug', do developer publish."
  echo "Configuration: 'SkipBuild', go directly steam release (USE WITH CARE! YOU MAY PUBLISH BROKEN / WRONG DEPOTS!)."
  exit 2
elif [ $# -gt 1 ]; then
  echo 1>&2 "$0: too many arguments"
  exit 2
fi

if [[ -z "${AW_STEAM_BUILD_ACCOUNT:=}" || -z "${AW_STEAM_BUILD_PASSWORD:=}" ]]; then
  echo "Environment variables AW_STEAM_BUILD_ACCOUNT and AW_STEAM_BUILD_PASSWORD must be defined"
  exit 2
fi

platform_run() {
  # bash with WSL runs build commands in some weird context. OTOH building on linux is probably fine.
  # TODO: Add logic here on when to do cmd \c
  echo cmd.exe /c "$@"
  cmd.exe /c "$@"
}

CONFIGURATION=$1
#ROOT=$(pwd)
#SOLUTION="${ROOT}\AssaultWing.sln"
ROOT=.
SOLUTION="AssaultWing.sln"

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
  platform_run dotnet build $SOLUTION --nologo --verbosity=quiet /p:Configuration=$CONFIGURATION "/p:Platform=Any Cpu"

  echo Built: */bin/*
}

if [[ "$CONFIGURATION" != "SkipBuild" ]]; then
  echo "Configuration=${CONFIGURATION}"
  raw_clean
  clean_build_setup
  build
else 
  echo "Skipping cleaning and building due to CONFIGURATION=${CONFIGURATION}"
fi

platform_run 'steambuild\builder\steamcmd.exe'  +login "$AW_STEAM_BUILD_ACCOUNT" "$AW_STEAM_BUILD_PASSWORD" +run_app_build '..\..\scripts\steam_build.vdf' +quit