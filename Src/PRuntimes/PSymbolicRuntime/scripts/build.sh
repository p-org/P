#!/usr/bin/env bash
NOCOLOR='\033[0m'
RED='\033[0;31m'
GREEN='\033[0;32m'
ORANGE='\033[0;33m'
set -e

pushd .
echo -e "${ORANGE} ---- Building PSym runtime ----${NOCOLOR}"
mvn clean initialize -q
mvn install -Dmaven.test.skip -q
popd

pushd .
echo -e "${ORANGE} ---- Building P ----${NOCOLOR}"
cd ../../../Bld/
./build.sh
popd
