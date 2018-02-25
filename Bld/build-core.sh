@echo off
pushd %~dp0
cd ..

REM Clean up submodules
git submodule foreach --recursive git reset --hard
git submodule foreach --recursive git clean -fdx
git clean -fdx

REM Initialize submodules
git submodule update --init --recursive

REM Place build dependencies
mkdir Ext\nupkgs
dotnet build Ext\Formula\Src\Extensions\FormulaCodeGeneratorTask\FormulaCodeGeneratorTask.csproj
dotnet pack Ext\PSharp\Source\TestingServices\TestingServices.csproj --output ..\..\..\nupkgs
dotnet pack Ext\Formula\Src\Core\Core.csproj --output ..\..\..\nupkgs

REM Run the build! :D
dotnet build P.sln

popd
