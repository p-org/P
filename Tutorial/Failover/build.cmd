set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

set pt=..\..\bld\drops\Release\x64\Binaries\pt.exe

%pc% /generate:C# /shared Main.p FaultTolerantMachine.p Safety.p 

%pc% /link /shared TestScript.p /r:Main.4ml

%pt% /psharp linker.dll

goto :eof
:noP
echo please run ..\..\bld\build release x64
exit 1
