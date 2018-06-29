@echo off
pushd %~dp0
cd ..

REM Clean up submodules
git submodule foreach --recursive git reset --hard
git submodule foreach --recursive git clean -fdx
git clean -fdx

REM Initialize submodules
git submodule update --init --recursive

REM Run the build! :D
dotnet publish -c Release P.sln

popd
