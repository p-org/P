set THISDIR=%~dp0
pushd %THISDIR%
set pc=..\..\..\..\Bld\drops\Release\x64\Binaries\pc.exe
if not exist "%pc%" goto :noP

REM set pt=..\..\..\..\Bld\drops\Release\x64\Binaries\pt.exe

REM %pc% /generate:C# /shared ..\CommonUtilities\Timer.p ..\CommonUtilities\TimerHeader.p /t:Timer.4ml /outputDir:..\CommonUtilities\ /profile

REM if NOT errorlevel 0 goto :eof

REM %pc% /generate:C# /shared ..\SMR\StateMachineReplicationHeader.p ..\SMR\StateMachineReplicationAbs.p /t:SMRAbs.4ml /outputDir:..\SMR\ /profile

REM if NOT errorlevel 0 goto :eof

%pc% /generate:C# /shared /t:datastructure.4ml .\DataStructuresHeader.p .\DataStructuresClient.p .\List.p .\HashSet.p .\TestDrivers.p .\DataStructuresSpec.p /r:..\SMR\SMRAbs.4ml /r:..\CommonUtilities\Timer.4ml /profile

if NOT errorlevel 0 goto :eof
%pc% /generate:C# /shared /link TestScript.p /r:datastructure.4ml /r:..\SMR\SMRAbs.4ml /r:..\CommonUtilities\Timer.4ml /profile

if NOT errorlevel 0 goto :eof


REM %pt% /psharp Test0.dll
REM if NOT errorlevel 0 goto :eof
REM %pt% /psharp Test1.dll
REM if NOT errorlevel 0 goto :eof
REM %pt% /psharp Test2.dll
REM if NOT errorlevel 0 goto :eof
REM %pt% /psharp Test3.dll
REM if NOT errorlevel 0 goto :eof
REM %pt% /psharp Test4.dll
REM if NOT errorlevel 0 goto :eof
goto :eof
:noP
echo please run ..\..\bld\build release x64

