pushd ..

# Initialize submodules
Write-Host "Initializing submodules .."  -ForegroundColor DarkGreen -BackgroundColor White
git submodule update --init --recursive

mvn clean compile -f ./Src/PRuntimes/PJavaRuntime/pom.xml

# Run the build! :D
dotnet build -c Release

$x = Get-Location
Write-Host "----------------------------------------------"  -ForegroundColor DarkRed -BackgroundColor White
Write-Host "P Compiler is located at $x\Bld\Drops\Release\Binaries\Pc.dll"  -ForegroundColor DarkGreen -BackgroundColor White
Write-Host "----------------------------------------------"  -ForegroundColor DarkRed -BackgroundColor White
popd
