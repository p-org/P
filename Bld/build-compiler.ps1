pushd (Join-Path $PSScriptRoot ..)

# Initialize submodules
git submodule update --init --recursive

# Run the build! :D
dotnet publish -c Release -r win-x64 .\Src\PCompiler\CommandLine\CommandLine.csproj

echo "Compiler located in Bld\Drops\Release\Binaries\win-x64\Pc.exe"

popd
