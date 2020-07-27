set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\Binaries\win-x64\pc.exe
if not exist "%pc%" goto :noP

%pc% -generate:Coyote -t:Hello ..\Timer\Timer.p ..\Env\Env.p Main.p Continue.p TestScript.p

if NOT errorlevel 0 goto :eof

%pc% -generate:C -t:Hello ..\Timer\Timer.p ..\Env\Env.p Main.p Continue.p TestScript.p

if NOT errorlevel 0 goto :eof

goto :eof
:noP
echo please build P
exit /b 1
