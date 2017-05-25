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

Configuration=Debug
Platform=x86
if [ $# -ne 2 ]; then
    echo "No configuration supplied. Falling back on default: Debug,x86"
else
    Configuration=$1
    Configuration="$(tr '[:lower:]' '[:upper:]' <<< ${Configuration:0:1})${Configuration:1}"
    Platform=$2
fi

echo Configuration is $Configuration, $Platform

git submodule update --init --recursive --remote

mono Bld/nuget.exe restore PLinux.sln

cd Ext/Zing

echo $MSBUILD Zing.sln /p:Platform=$Platform /p:Configuration=$Configuration
$MSBUILD ZING.sln /p:Platform=$Platform /p:Configuration=$Configuration

if [ $? -ne  0 ]; then
    echo "Zing build failed. Exiting..."
    popd
    exit 2
fi

BinaryDrop=../../Bld/Drops/$Configuration/$Platform/Binaries

if [ ! -f $BinaryDrop ]; then
    mkdir -p $BinaryDrop
fi

filesToCopy="zc/bin/$Platform/$Configuration/zc.exe
             ZingExplorer/bin/$Platform/$Configuration/ZingExplorer.dll
             Zinger/bin/$Platform/$Configuration/Zinger.exe
             Microsoft.Zing/bin/$Platform/$Configuration/Microsoft.Zing.dll
             Microsoft.Zing.Runtime/bin/$Platform/$Configuration/Microsoft.Zing.Runtime.dll
             Microsoft.Zing/bin/$Platform/$Configuration/Microsoft.Comega.dll
             Microsoft.Zing/bin/$Platform/$Configuration/Microsoft.Comega.Runtime.dll
             Resources/external/CCI/System.Compiler.dll
             Resources/external/CCI/System.Compiler.Framework.dll
             Resources/external/CCI/System.Compiler.Runtime.dll
             DelayingSchedulers/CustomDelayingScheduler/bin/$Platform/$Configuration/CustomDelayingScheduler.dll
             DelayingSchedulers/RandomDelayingScheduler/bin/$Platform/$Configuration/RandomDelayingScheduler.dll
             DelayingSchedulers/RoundRobinDelayingScheduler/bin/$Platform/$Configuration/RoundRobinDelayingScheduler.dll
             DelayingSchedulers/RunToCompletionDelayingScheduler/bin/$Platform/$Configuration/RunToCompletionDelayingScheduler.dll" 

for i in $filesToCopy
do
    cp $i $BinaryDrop
done

cd ../..

echo $MSBUILD ext/Formula/src/Extensions/FormulaCodeGeneratorTask/FormulaCodeGeneratorTask.csproj /p:Platform=$Platform /p:Configuration=$Configuration
$MSBUILD ext/Formula/src/Extensions/FormulaCodeGeneratorTask/FormulaCodeGeneratorTask.csproj /p:Platform=$Platform /p:Configuration=$Configuration

echo $MSBUILD PLinux.sln /p:Platform=$Platform /p:Configuration=$Configuration
$MSBUILD PLinux.sln /p:Platform=$Platform /p:Configuration=$Configuration /t:Clean
$MSBUILD PLinux.sln /p:Platform=$Platform /p:Configuration=$Configuration

popd

pushd $SCRIPTPATH
mkdir -p build; cd build; cmake ../../Src; make
popd


