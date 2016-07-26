@echo off
setlocal
pushd %~dp0
cd ..
goto :start

:help
echo Usage: build [debug|release] [x64|x86] [nosync] [clean] 
echo nosync - do not update the git submodules.
echo clean - only clean the build

goto :exit

:start
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
set Configuration=Debug
set Platform=x86
set NoSync=
set CleanOnly=

:parseargs
if "%1"=="Debug" set Configuration=Debug
if "%1"=="Release" set Configuration=Release
if "%1"=="x86" set Platform=x86
if "%1"=="x64" set Platform=x64
if "%1"=="nosync" set NoSync=true
if "%1"=="clean" set CleanOnly=true
if "%1"=="" goto :step2
if "%1"=="/?" goto :help
if "%1"=="/h" goto :help
if "%1"=="/help" goto :help
if "%1"=="help" goto :help
shift
goto :parseargs

:step2

if "%NoSync%"=="true" goto :nosync

git submodule init
git submodule update

:nosync
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
             DelayingSchedulers\RunToCompletionDelayingScheduler\bin\%Platform%\Release\RunToCompletionDelayingScheduler.dll
	     DelayingSchedulers\SealingScheduler\bin\%Platform%\Release\SealingScheduler.dll) do (
             
    copy %%i %BinaryDrop%
)
   
cd ..\..
REM this code fixes a problem in MIDL compile by forcing recompile of these files for each configuration.
del Src\PrtDist\Core\NodeManager_c.c
del Src\PrtDist\Core\NodeManager_s.c

echo msbuild P.sln /p:Platform=%Platform% /p:Configuration=%Configuration%
msbuild  P.sln /p:Platform=%Platform% /p:Configuration=%Configuration% /t:Clean

if "%CleanOnly%"=="true" goto :exit
msbuild P.sln /p:Platform=%Platform% /p:Configuration=%Configuration% /p:SOLVER=NOSOLVER

:exit
popd