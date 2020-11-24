pushd ..

# Initialize submodules
Write-Host "Initializing submodules .."  -ForegroundColor DarkGreen -BackgroundColor White
git submodule update --init --recursive

# Run the build! :D
dotnet build -c Release

pushd Src/PRuntimes/RvmRuntime
mvn install
popd

Copy-Item -Path "Src/PRuntimes/RvmRuntime/target/*.jar" -Destination "Bld/Drops"

$x = Get-Location
Write-Host "----------------------------------------------"  -ForegroundColor DarkRed -BackgroundColor White
Write-Host "P Compiler is located at $x\Bld\Drops\Release\Binaries\Pc.dll"  -ForegroundColor DarkGreen -BackgroundColor White
Write-Host "----------------------------------------------"  -ForegroundColor DarkRed -BackgroundColor White
popd
