set THISDIR=%~dp0
pushd %THISDIR%

set pc=..\..\Bld\Drops\Release\Binaries\win-x64\pc.exe
if not exist "%pc%" goto :noP

%pc% -generate:Coyote -t:FailOver Main.p FaultTolerantMachine.p TestScript.p

if NOT errorlevel 0 goto :eof

%pc% -generate:C -t:FailOver Main.p FaultTolerantMachine.p TestScript.p

if NOT errorlevel 0 goto :eof

goto :eof
:noP
echo please build P
exit /b 1
