set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

%pc% /generate:C# /shared Timer.p /t:Timer.4ml

goto :eof
:noP
echo please run ..\..\bld\build release x64
exit /b 1
