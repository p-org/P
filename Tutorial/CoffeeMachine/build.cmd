set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

set pt=..\..\bld\drops\Release\x64\Binaries\pt.exe

msbuild /p:Platform=x64 /p:Configuration=Release CoffeeMachine.vcxproj

%pc% /generate:C# /shared CoffeeMachine.p Main.p Safety.p /t:CoffeeMachine.4ml /r:..\..\Src\Samples\Timer\Timer.4ml

if NOT errorlevel 0 goto :eof

%pc% /link /shared TestScript.p /r:CoffeeMachine.4ml /r:..\..\Src\Samples\Timer\Timer.4ml

if NOT errorlevel 0 goto :eof

%pt% linker.dll

goto :eof
:noP
echo please run ..\..\bld\build release x64
exit 1
