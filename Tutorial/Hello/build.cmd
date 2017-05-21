set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

set pt=..\..\bld\drops\Release\x64\Binaries\pt.exe

msbuild /p:Platform=x64 /p:Configuration=Release Hello.sln

%pc% /generate:C# /shared Main.p /r:..\..\Src\Samples\Timer\PGenerated\timer.4ml

%pc% /link /shared /r:Main.4ml /r:..\..\Src\Samples\Timer\PGenerated\timer.4ml

%pt% /psharp linker.dll

goto :eof
:noP
echo please run ..\..\bld\build release x64
exit 1
