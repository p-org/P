SCRIPT=$0
SCRIPTPATH=$(dirname "$SCRIPT") #Absolute path of script
pushd $SCRIPTPATH
cd ..

echo ============= Building P SDK ===============

type xbuild >/dev/null 2>&1 || { echo >&2 "xbuild is not installed. Exiting..."; popd; exit 1; }

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

git submodule init
git submodule update
cd Ext/Zing

echo xbuild  Zing.sln /p:Platform=$Platform /p:Configuration=Release
xbuild  Zing.sln /p:Platform=$Platform /p:Configuration=Release

if [ $? -ne  0 ]; then
    echo "Zing build failed. Exiting..."
    popd
    exit 2
fi

set BinaryDrop=../../Bld/Drops/$Configuration/$Platform/Binaries

if [ ! -f $BinaryDrop ]; then
    mkdir -p $BinaryDrop
fi

set filesToCopy="zc/bin/$Platform/Release/zc.exe
             ZingExplorer/bin/$Platform/Release/ZingExplorer.dll
             Zinger/bin/$Platform/Release/Zinger.exe
             Microsoft.Zing/bin/$Platform/Release/Microsoft.Zing.dll
             Microsoft.Zing.Runtime/bin/$Platform/Release/Microsoft.Zing.Runtime.dll
             Microsoft.Zing/bin/$Platform/Release/Microsoft.Comega.dll
             Microsoft.Zing/bin/$Platform/Release/Microsoft.Comega.Runtime.dll
             Resources/external/CCI/System.Compiler.dll
             Resources/external/CCI/System.Compiler.Framework.dll
             Resources/external/CCI/System.Compiler.Runtime.dll
             DelayingSchedulers/CustomDelayingScheduler/bin/$Platform/Release/CustomDelayingScheduler.dll
             DelayingSchedulers/RandomDelayingScheduler/bin/$Platform/Release/RandomDelayingScheduler.dll
             DelayingSchedulers/RoundRobinDelayingScheduler/bin/$Platform/Release/RoundRobinDelayingScheduler.dll
             DelayingSchedulers/RunToCompletionDelayingScheduler/bin/$Platform/Release/RunToCompletionDelayingScheduler.dll" 


for i in $filesToCopy
do
    cp $i $BinaryDrop
done
   
cd ../..

# This code fixes a problem in MIDL compile by forcing recompile of these files for each configuration.
rm Src/PrtDist/Core/NodeManager_c.c
rm Src/PrtDist/Core/NodeManager_s.c

echo xbuild P.sln /p:Platform=$Platform /p:Configuration=$Configuration
xbuild  P.sln /p:Platform=$Platform /p:Configuration=$Configuration /t:Clean
xbuild P.sln /p:Platform=$Platform /p:Configuration=$Configuration

popd

mkdir -p build; cd build; cmake ../../Src; make


