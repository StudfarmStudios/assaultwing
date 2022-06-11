#!/bin/bash

if [ -z "$BASH" ] ;then echo "Please run this script $0 with bash"; exit 1; fi

set -euo pipefail
IFS=$'\n\t'

if [ $# -lt 1 ]; then
  echo "This is a bash script built to work on plain Linux (TBD) OR Windows drive mounted on WSL2."
  echo "Usage: $0 [Configuration]"
  echo "Configuration: 'Release' for release. If 'Debug', do developer publish."
  exit 2
elif [ $# -gt 1 ]; then
  echo 1>&2 "$0: too many arguments"
  exit 2
fi

platform_run() {
  # bash with WSL runs build commands in some weird context. OTOH building on linux is probably fine.
  # TODO: Add logic here on when to do cmd \c
  cmd.exe /c "$@"
}

CONFIGURATION=$1
#ROOT=$(pwd)
#SOLUTION="${ROOT}\AssaultWing.sln"
ROOT=.
SOLUTION="AssaultWing.sln"

echo Remove "*/bin/*": */bin/*
rm -rf */bin/*

echo dotnet restore
platform_run dotnet restore $SOLUTION

echo dotnet clean
platform_run dotnet clean $SOLUTION --nologo --verbosity=quiet


# It seems we need to define build profiles to build for Linux & Mac here. https://github.com/dotnet/sdk/issues/14281#issuecomment-863499124
# /target:Publish
echo dotnet build
platform_run dotnet build $SOLUTION --nologo --verbosity=quiet /p:Configuration=$CONFIGURATION "/p:Platform=Any Cpu"

echo Built: */bin/*

