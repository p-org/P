set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

set pt=..\..\bld\drops\Release\x64\Binaries\pt.exe

msbuild /p:Platform=x64 /p:Configuration=Release Sample0.sln

%pc% /generate:C# /shared Main.p PingPong.p Safety.p Timer.p

%pc% /link /shared /r:Main.4ml

%pt% /psharp linker.dll

goto :eof
:noP
echo please run ..\..\bld\build release x64
exit 1