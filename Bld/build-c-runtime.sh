#!/usr/bin/env bash

echo "Initializing submodules"
# Initialize submodules
pushd ..
git submodule update --init --recursive
popd

# Build the C runtime!
mkdir -p build
cd build
cmake ../../Src
make


