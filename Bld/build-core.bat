@echo off
pushd %~dp0
cd ..

git submodule foreach --recursive git reset --hard
git submodule foreach --recursive git clean -x -f -d
git clean -fdx

dotnet build Ext\Formula\Src\Extensions\FormulaCodeGeneratorTask\FormulaCodeGeneratorTask.csproj
dotnet pack Ext\Formula\Src\Core\Core.csproj --output ..\..\..\nupkgs
dotnet pack Ext\PSharp --output ..\nupkgs

dotnet build P.sln

:exit
popd