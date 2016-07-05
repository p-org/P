@echo off
setlocal
pushd %~dp0
cd ..

echo ============= Building P SDK on %COMPUTERNAME% ===============

set MSBuildPath=
for /F "usebackq tokens=1,2* delims= " %%i in (`reg query HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\14.0 -v MSBuildToolsPath`) do (
   if "%%i" == "MSBuildToolsPath" set MSBuildPath=%%k
)

if not "%MSBuildPath%"=="" goto :step2

echo MSBUILD 14.0 does not appear to be installed.
echo No info found in HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\14.0
goto :eof

:step2
set TAIL=%MSBuildPath:~-6%
if "[%TAIL%]" == "[amd64\]" set MSBuildPath=%MSBuildPath:~0,-6%"
set PATH=%PATH%;%MSBuildPath%
set Configuration=%1
if "%Configuration%"=="" set Configuration=Debug
set Platform=%2
if "%Platform%"=="" set Platform=x86

git submodule init
git submodule update
cd ext\zing

echo msbuild  Zing.sln /p:Platform=%Platform% /p:Configuration=Release
msbuild  Zing.sln /p:Platform=%Platform% /p:Configuration=Release
if ERRORLEVEL 1 goto :exit

set BinaryDrop=..\..\Bld\Drops\%Configuration%\%Platform%\Binaries
if NOT exist %BinaryDrop% mkdir %BinaryDrop%

for %%i in (zc\bin\%Platform%\Release\zc.exe
             ZingExplorer\bin\%Platform%\Release\ZingExplorer.dll
             Zinger\bin\%Platform%\Release\Zinger.exe
             Microsoft.Zing\bin\%Platform%\Release\Microsoft.Zing.dll
             Microsoft.Zing.Runtime\bin\%Platform%\Release\Microsoft.Zing.Runtime.dll
             Microsoft.Zing\bin\%Platform%\Release\Microsoft.Comega.dll
             Microsoft.Zing\bin\%Platform%\Release\Microsoft.Comega.Runtime.dll
             Resources\external\CCI\System.Compiler.dll
             Resources\external\CCI\System.Compiler.Framework.dll
             Resources\external\CCI\System.Compiler.Runtime.dll
             DelayingSchedulers\CustomDelayingScheduler\bin\%Platform%\Release\CustomDelayingScheduler.dll
             DelayingSchedulers\RandomDelayingScheduler\bin\%Platform%\Release\RandomDelayingScheduler.dll
             DelayingSchedulers\RoundRobinDelayingScheduler\bin\%Platform%\Release\RoundRobinDelayingScheduler.dll
             DelayingSchedulers\RunToCompletionDelayingScheduler\bin\%Platform%\Release\RunToCompletionDelayingScheduler.dll) do (
             
    copy %%i %BinaryDrop%
)
   
cd ..\..
REM this code fixes a problem in MIDL compile by forcing recompile of these files for each configuration.
del Src\PrtDist\Core\NodeManager_c.c
del Src\PrtDist\Core\NodeManager_s.c

echo msbuild P.sln /p:Platform=%Platform% /p:Configuration=%Configuration%
msbuild  P.sln /p:Platform=%Platform% /p:Configuration=%Configuration% /t:Clean
msbuild P.sln /p:Platform=%Platform% /p:Configuration=%Configuration%

:exit
popd