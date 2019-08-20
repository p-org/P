param(
    [ValidateSet("netcoreapp2.1","net46")]
    [string]$framework="netcoreapp2.1"
)

$pc_path = $PSScriptRoot + "\..\..\..\Bld\Drops\Release\Binaries\win-x64\Pc.exe"

& $pc_path .\PSrc\Client.p .\PSrc\Coordinator.p .\PSrc\Participant.p .\PSrc\Events.p .\PSrc\Spec.p .\PSrc\TestDriver.p .\PSrc\Timer.p -generate:P# -t:Main

dotnet publish -f $framework .\TwoPhaseCommit.csproj
