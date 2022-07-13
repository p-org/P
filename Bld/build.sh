#!/usr/bin/env bash
NOCOLOR='\033[0m'
RED='\033[0;31m'
GREEN='\033[0;32m'
ORANGE='\033[0;33m'
set -e

echo -e "${ORANGE} ---- Fetching git submodules ----${NOCOLOR}"
pushd ..
# Initialize submodules
git submodule update --init --recursive

echo -e "${ORANGE} ---- Building the Java P runtime ----${NOCOLOR}"
mvn clean compile -f ./Src/PRuntimes/PJavaRuntime/pom.xml

echo -e "${ORANGE} ---- Building the PCompiler ----${NOCOLOR}"
# Run the build!

dotnet build -c Release

echo -e "${GREEN} ----------------------------------${NOCOLOR}"
echo -e "${GREEN} P Compiler located in ${PWD}/Bld/Drops/Release/Binaries/net6.0/P.dll${NOCOLOR}"
echo -e "${GREEN} ----------------------------------${NOCOLOR}"


echo -e "${GREEN} Shortcuts:: (add the following lines (aliases) to your bash_profile) ${NOCOLOR}"
echo -e "${GREEN} ----------------------------------${NOCOLOR}"
echo -e "${ORANGE} alias pcl='dotnet ${PWD}/Bld/Drops/Release/Binaries/net6.0/P.dll'${NOCOLOR}"
echo -e "${GREEN} ----------------------------------${NOCOLOR}"
popd

