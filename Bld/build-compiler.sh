#!/usr/bin/env bash

set -e

cd "$(dirname "${0}")/.."

# Initialize submodules
git submodule update --init --recursive

# Run the build!
dotnet publish -c Release ./Src/Pc/CommandLine/CommandLine.csproj

echo "Compiler located in Bld/Drops/Release/Binaries/Pc.dll"
