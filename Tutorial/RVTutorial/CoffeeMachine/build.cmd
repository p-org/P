set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\..\bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

set pt=..\..\..\bld\drops\Release\x64\Binaries\pt.exe

%pc% /generate:C# /shared CoffeeMachine.p CoffeeMachineController.p Timer.p User.p Main.p Safety.p /t:CoffeeMachine.4ml

if NOT errorlevel 0 goto :eof

%pc% /generate:C# /link /shared TestScript.p /r:CoffeeMachine.4ml

if NOT errorlevel 0 goto :eof

%pt% /psharp Test0.dll

%pt% /psharp Test1.dll

%pt% /psharp Test2.dll

goto :eof
:noP
echo please run ..\..\..\bld\build release x64
exit /b 1
