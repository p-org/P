set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

set pt=..\..\bld\drops\Release\x64\Binaries\pt.exe

%pc% /generate:C# /shared Main.p FaultTolerantMachine.p Safety.p /t:Failover.4ml

%pc% /link /shared TestScript.p /r:Failover.4ml

%pt% /psharp Test0.dll

goto :eof
:noP
echo please run ..\..\bld\build release x64
exit 1
