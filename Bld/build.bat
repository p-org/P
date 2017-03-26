@echo off
setlocal
pushd %~dp0
cd ..
goto :start

:help
echo Usage: build [debug|release] [x64|x86] [nosync] [clean|noclean] 
echo nosync - do not update the git submodules.
echo clean - only clean the build
echo noclean - do not clean the build, so do incemental build

goto :exit

:start
echo ============= Building P SDK on %COMPUTERNAME% ===============

Bld\nuget restore P.sln

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
set Build64=0
if "[%TAIL%]" == "[amd64\]" set Build64=1
set PATH=%MSBuildPath%;%PATH%
set Configuration=Debug
set Platform=x86
set NoSync=
set CleanOnly=
set NoClean=
set SubmoduleOutOfDate=false

:parseargs
if /I "%1"=="debug" set Configuration=Debug
if /I "%1"=="release" set Configuration=Release
if /I "%1"=="x86" set Platform=x86
if /I "%1"=="x64" set Platform=x64
if /I "%1"=="nosync" set NoSync=true
if /I "%1"=="clean" set CleanOnly=true
if /I "%1"=="noclean" set NoClean=true
if /I "%1"=="" goto :initsub
if /I "%1"=="/?" goto :help
if /I "%1"=="/h" goto :help
if /I "%1"=="/help" goto :help
if /I "%1"=="help" goto :help
shift
goto :parseargs

:initsub
if exist "Ext\PSharp\README.md" (
    if exist "Ext\Formula\README.md" (
        if exist "EXT\Zing\README.md" (
            goto :updatesub
        )
    )
)

echo ### Initializing your submodules 
git submodule init
git submodule update --recursive
goto :sync

:checksubmodule
for /f "usebackq tokens=1,2*" %%i in (`git submodule summary %1`) do (
  if "%%j"=="%1" echo #### Submodule is out of date: %1 & set SubmoduleOutOfDate=true  
)

goto :eof

:updatesub
if "%NoSync%"=="true" goto :nosync

echo ### Updating your submodules 
call :checksubmodule Ext/Formula
call :checksubmodule Ext/Zing
call :checksubmodule Ext/PSharp

if "%SubmoduleOutOfDate%"=="false" goto :nosync

:sync
echo ### Fixing your submodules so they are up to date...
git submodule sync --recursive
git submodule update --init --recursive
goto :nosync

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

REM Build PSharp
cd ext\PSharp
..\..\Bld\nuget restore PSharp.sln
echo msbuild PSharp.sln /p:Platform="Any CPU" /p:Configuration=%Configuration%
msbuild  PSharp.sln /p:Platform="Any CPU" /p:Configuration=%Configuration%
if ERRORLEVEL 1 goto :exit

cd ..\..

if "%NoClean%"=="true" goto :build
echo msbuild P.sln /p:Platform=%Platform% /p:Configuration=%Configuration%  /t:Clean
msbuild  P.sln /p:Platform=%Platform% /p:Configuration=%Configuration% /t:Clean

:build

set FormulaCodeGeneratorTaskPlatform=x86
if "%Build64%"=="1" set FormulaCodeGeneratorTaskPlatform=x64
echo msbuild FormulaCodeGeneratorTask /p:Platform=%FormulaCodeGeneratorTaskPlatform% /p:Configuration=%Configuration%
msbuild  ext\Formula\src\Extensions\FormulaCodeGeneratorTask\FormulaCodeGeneratorTask.csproj /p:Platform=%FormulaCodeGeneratorTaskPlatform% /p:Configuration=%Configuration%

if "%CleanOnly%"=="true" goto :exit

call :StartTimer
msbuild P.sln /p:Platform=%Platform% /p:Configuration=%Configuration% /p:SOLVER=NOSOLVER
call :StopTimer
call :DisplayTimerResult

goto :exit

:StartTimer
:: Store start time
set StartTIME=%TIME%
for /f "usebackq tokens=1-4 delims=:., " %%f in (`echo %StartTIME: =0%`) do set /a Start100S=1%%f*360000+1%%g*6000+1%%h*100+1%%i-36610100
goto :EOF

:StopTimer
:: Get the end time
set StopTIME=%TIME%
for /f "usebackq tokens=1-4 delims=:., " %%f in (`echo %StopTIME: =0%`) do set /a Stop100S=1%%f*360000+1%%g*6000+1%%h*100+1%%i-36610100
:: Test midnight rollover. If so, add 1 day=8640000 1/100ths secs
if %Stop100S% LSS %Start100S% set /a Stop100S+=8640000
set /a TookTime=%Stop100S%-%Start100S%
set TookTimePadded=0%TookTime%
goto :EOF

:DisplayTimerResult
:: Show timer start/stop/delta
echo Started: %StartTime%
echo Stopped: %StopTime%
echo Elapsed: %TookTime:~0,-2%.%TookTimePadded:~-2% seconds
goto :EOF

:exit
popd
