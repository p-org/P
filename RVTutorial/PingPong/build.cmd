set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

set pt=..\..\bld\drops\Release\x64\Binaries\pt.exe

#msbuild /p:Platform=x64 /p:Configuration=Release PingPong.vcxproj

%pc% /generate:C# /shared ..\Timer\Timer.p /t:Timer.4ml /outputDir:..\Timer

if NOT errorlevel 0 goto :eof

%pc% /generate:C# /shared ..\Env\Env.p /t:Env.4ml /outputDir:..\Env

if NOT errorlevel 0 goto :eof

%pc% /generate:C# /shared Main.p PingPong.p Safety.p Liveness.p /t:PingPong.4ml /r:..\Timer\timer.4ml /r:..\Env\env.4ml

if NOT errorlevel 0 goto :eof

%pc% /generate:C# /link /shared TestScript.p /r:PingPong.4ml /r:..\Timer\timer.4ml /r:..\Env\env.4ml

if NOT errorlevel 0 goto :eof

%pt% /psharp Test0.dll

%pt% /psharp Test1.dll

goto :eof
:noP
echo please run ..\..\bld\build release x64
exit /b 1
