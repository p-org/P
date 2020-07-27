set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\Binaries\win-x64\pc.exe
if not exist "%pc%" goto :noP

%pc% -generate:Coyote -t:CoffeeMachine Timer.p User.p Main.p CoffeeMachine.p Safety.p CoffeeMachineController.p TestScript.p

if NOT errorlevel 0 goto :eof

%pc% -generate:C -t:CoffeeMachine Timer.p User.p Main.p CoffeeMachine.p Safety.p CoffeeMachineController.p TestScript.p

if NOT errorlevel 0 goto :eof

goto :eof
:noP
echo please build P
exit /b 1
