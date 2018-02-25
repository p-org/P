@echo off
pushd %~dp0
cd ..

git submodule foreach --recursive git reset --hard
git submodule foreach --recursive git clean -fdx
git clean -fdx

git submodule update --init --recursive

mkdir Ext/nupkgs
dotnet build Ext/Formula/Src/Extensions/FormulaCodeGeneratorTask/FormulaCodeGeneratorTask.csproj
dotnet pack Ext/PSharp/Source/TestingServices/TestingServices.csproj --output ../../../nupkgs
dotnet pack Ext/Formula/Src/Core/Core.csproj --output ../../../nupkgs

dotnet build P.sln

popd
