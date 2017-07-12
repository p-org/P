#!/bin/bash

SCRIPT=$0
SCRIPTPATH=$(dirname "$SCRIPT") #Absolute path of script
pushd $SCRIPTPATH
cd ..

export MONO_IOMAP=case

echo ========= Detecting Build System ===========

if [ -z "$MSBUILD" ]; then
    if $(type msbuild > /dev/null 2>&1); then
        MSBUILD=msbuild
    elif $(type xbuild > /dev/null 2>&1); then
        MSBUILD=xbuild
    else
        echo >&2 "msbuild or xbuild are not installed. Exiting...";
        popd;
        exit 1;
    fi
fi

echo Using $MSBUILD to build solutions

echo ============= Building P SDK ===============

Configuration=Release
Platform=x64
if [ $# -ne 2 ]; then
    echo "No configuration supplied. Falling back on default: Release,x64"
else
    Configuration=$1
    Configuration="$(tr '[:lower:]' '[:upper:]' <<< ${Configuration:0:1})${Configuration:1}"
    Platform=$2
fi

echo Configuration is $Configuration, $Platform

git submodule update --init --recursive --remote

mono Bld/nuget.exe restore PLinux.sln

echo $MSBUILD ext/Formula/src/Extensions/FormulaCodeGeneratorTask/FormulaCodeGeneratorTask.csproj /p:Platform=$Platform /p:Configuration=$Configuration
$MSBUILD ext/Formula/src/Extensions/FormulaCodeGeneratorTask/FormulaCodeGeneratorTask.csproj /p:Platform=$Platform /p:Configuration=$Configuration

echo $MSBUILD PLinux.sln /p:Platform=$Platform /p:Configuration=$Configuration
$MSBUILD PLinux.sln /p:Platform=$Platform /p:Configuration=$Configuration /t:Clean
$MSBUILD PLinux.sln /p:Platform=$Platform /p:Configuration=$Configuration

popd

pushd $SCRIPTPATH
mkdir -p build; cd build; cmake ../../Src; make
popd


