#!/usr/bin/env bash
NOCOLOR='\033[0m'
RED='\033[0;31m'
GREEN='\033[0;32m'
ORANGE='\033[0;33m'
set -e

BLD_PATH=$(dirname $0)
pushd $BLD_PATH/..

echo -e "${ORANGE} ---- Fetching git submodules ----${NOCOLOR}"

# Initialize submodules
git submodule update --init --recursive

echo -e "${ORANGE} ---- Building the Java P runtime ----${NOCOLOR}"
mvn compile -q -f ./Src/PRuntimes/PJavaRuntime/pom.xml

echo -e "${ORANGE} ---- Building the PCompiler ----${NOCOLOR}"
# Run the build!

output=`dotnet build -c Release -v q 2>&1` || echo $output

if [ $? -eq 0 ]; then
    echo -e "${GREEN} ----------------------------------${NOCOLOR}"
    echo -e "${GREEN} P Compiler located in ${PWD}/Bld/Drops/Release/Binaries/net8.0/p.dll${NOCOLOR}"
    echo -e "${GREEN} ----------------------------------${NOCOLOR}"
    echo -e "${GREEN} ----------------------------------${NOCOLOR}"
    echo -e "${GREEN} Shortcuts:: (add the following lines (aliases) to your bash_profile) ${NOCOLOR}"
    echo -e "${ORANGE} alias pl='dotnet ${PWD}/Bld/Drops/Release/Binaries/net8.0/p.dll'${NOCOLOR}"
    echo -e "${GREEN} ----------------------------------${NOCOLOR}"
fi

popd

