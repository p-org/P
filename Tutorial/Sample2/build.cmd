set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

set pt=..\..\bld\drops\Release\x64\Binaries\pt.exe

msbuild /p:Platform=x64 /p:Configuration=Release Sample2.sln

%pc% /generate:C# /shared Client.p Lock.p Main.p

%pc% /link /shared /r:Client.4ml

%pt% linker.dll

goto :eof
:noP
echo please run ..\..\bld\build release x64
exit 1