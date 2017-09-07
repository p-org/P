set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

%pc% /generate:C# /shared Env.p /t:Env.4ml

goto :eof
:noP
echo please run ..\..\bld\build release x64
exit /b 1
