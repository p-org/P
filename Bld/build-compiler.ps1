pushd (Join-Path $PSScriptRoot ..)

# Initialize submodules
git submodule update --init --recursive

# Run the build! :D
dotnet publish -c Release .\Src\PCompiler\CommandLine\CommandLine.csproj

echo "Compiler located in Bld\Drops\Release\Binaries\Pc.exe"

popd
