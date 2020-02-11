set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

set pt=..\..\bld\drops\Release\x64\Binaries\pt.exe

msbuild /p:Platform=x64 /p:Configuration=Release CoarseGrainedLocking.vcxproj

%pc% /generate:C# /shared Client.p Lock.p Main.p /t:Client.4ml

if NOT errorlevel 0 goto :eof

%pc% /generate:C# /link /shared /r:Client.4ml

if NOT errorlevel 0 goto :eof

%pt% /coyote linker.dll

goto :eof
:noP
echo please run ..\..\bld\build release x64
exit /b 1
