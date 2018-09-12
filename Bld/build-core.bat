@echo off
pushd %~dp0
cd ..

REM Initialize submodules
git submodule update --init --recursive

REM Run the build! :D
dotnet publish -c Release -r win-x64 P.sln

echo Compiler located in Bld\Drops\Release\AnyCPU\Binaries\win-x64\Pc.exe

popd
