set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

set pt=..\..\bld\drops\Release\x64\Binaries\pt.exe

msbuild /p:Platform=x64 /p:Configuration=Release PingPong.vcxproj

%pc% /generate:C# /shared Main.p PingPong.p Safety.p Liveness.p /r:..\Timer\timer.4ml

%pc% /link /shared TestScript.p /r:Main.4ml /r:..\Timer\timer.4ml

%pt% /psharp linker.dll

goto :eof
:noP
echo please run ..\..\bld\build release x64
exit 1
