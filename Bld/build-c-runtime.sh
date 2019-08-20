#!/usr/bin/env bash

cd "$(dirname "${0}")"

# Initialize submodules
pushd ..
git submodule update --init --recursive
popd

# Build the C runtime!
mkdir -p build
cd build
cmake ../../Src
make
