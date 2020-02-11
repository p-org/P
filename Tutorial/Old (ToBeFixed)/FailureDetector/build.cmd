set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

set pt=..\..\bld\drops\Release\x64\Binaries\pt.exe

%pc% /generate:C# /shared FailureDetector.p Timer.p TestDriver.p /t:FailureDetector.4ml

if NOT errorlevel 0 goto :eof

%pc% /generate:C# /link /shared TestScript.p /r:FailureDetector.4ml

if NOT errorlevel 0 goto :eof

%pt% /coyote Test0.dll

goto :eof
:noP
echo please run ..\..\bld\build release x64
exit /b 1
