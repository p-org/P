@echo off
SETLOCAL ENABLEDELAYEDEXPANSION 
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

set MSBuildPath=
for /F "usebackq tokens=1* delims=" %%i in (`where msbuild`) do (
   if "!MSBuildPath!"=="" set MSBuildPath=%%i
)

if not "%MSBuildPath%"=="" goto :step2

echo msbuild does not appear to be installed.
goto :eof

:step2
echo Found msbuild here: "%MSBuildPath%"

for /F "usebackq tokens=1* delims=: " %%i in (`corflags "%MSBuildPath%"`) do (
   if "%%i"=="32BITREQ" set MSBuild32Bit=%%j
)

set MsBuild64=1
if "%MSBuild32Bit%" == "1" set MsBuild64=0
set PBuildConfiguration=Debug
set PBuildPlatform=x86
set NoSync=
set CleanOnly=
set NoClean=
set SubmoduleOutOfDate=false

:parseargs
if /I "%1"=="debug" set PBuildConfiguration=Debug
if /I "%1"=="release" set PBuildConfiguration=Release
if /I "%1"=="x86" set PBuildPlatform=x86
if /I "%1"=="x64" set PBuildPlatform=x64
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
git submodule update
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

echo msbuild  Zing.sln /p:Platform=%PBuildPlatform% /p:Configuration=Release
msbuild  Zing.sln /p:Platform=%PBuildPlatform% /p:Configuration=Release
if ERRORLEVEL 1 goto :exit

set BinaryDrop=..\..\Bld\Drops\%PBuildConfiguration%\%PBuildPlatform%\Binaries
if NOT exist %BinaryDrop% mkdir %BinaryDrop%

for %%i in (zc\bin\%PBuildPlatform%\Release\zc.exe
             ZingExplorer\bin\%PBuildPlatform%\Release\ZingExplorer.dll
             Zinger\bin\%PBuildPlatform%\Release\Zinger.exe
             Microsoft.Zing\bin\%PBuildPlatform%\Release\Microsoft.Zing.dll
             Microsoft.Zing.Runtime\bin\%PBuildPlatform%\Release\Microsoft.Zing.Runtime.dll
             Microsoft.Zing\bin\%PBuildPlatform%\Release\Microsoft.Comega.dll
             Microsoft.Zing\bin\%PBuildPlatform%\Release\Microsoft.Comega.Runtime.dll
             Resources\external\CCI\System.Compiler.dll
             Resources\external\CCI\System.Compiler.Framework.dll
             Resources\external\CCI\System.Compiler.Runtime.dll
             DelayingSchedulers\CustomDelayingScheduler\bin\%PBuildPlatform%\Release\CustomDelayingScheduler.dll
             DelayingSchedulers\RandomDelayingScheduler\bin\%PBuildPlatform%\Release\RandomDelayingScheduler.dll
             DelayingSchedulers\RoundRobinDelayingScheduler\bin\%PBuildPlatform%\Release\RoundRobinDelayingScheduler.dll
             DelayingSchedulers\RunToCompletionDelayingScheduler\bin\%PBuildPlatform%\Release\RunToCompletionDelayingScheduler.dll
	     DelayingSchedulers\SealingScheduler\bin\%PBuildPlatform%\Release\SealingScheduler.dll) do (
             
    copy %%i %BinaryDrop%
)
   
cd ..\..

if "%NoClean%"=="true" goto :build
echo msbuild P.sln /p:Platform=%PBuildPlatform% /p:Configuration=%PBuildConfiguration%  /t:Clean
msbuild  P.sln /p:Platform=%PBuildPlatform% /p:Configuration=%PBuildConfiguration% /t:Clean
msbuild  Ext\PSharp\PSharp.sln /p:Platform="Any CPU" /p:Configuration=%PBuildConfiguration% /t:Clean

if "%CleanOnly%"=="true" goto :exit

:PSharp
REM Build PSharp
cd ext\PSharp
..\..\Bld\nuget restore -configfile NuGet.config PSharp.sln
echo msbuild PSharp.sln /p:Platform="Any CPU" /p:Configuration=%PBuildConfiguration%
msbuild  PSharp.sln /p:Platform="Any CPU" /p:Configuration=%PBuildConfiguration%
if ERRORLEVEL 1 goto :exit

cd ..\..

:build

set FormulaCodeGeneratorTaskPlatform=x86
if "%MsBuild64%"=="1" set FormulaCodeGeneratorTaskPlatform=x64
echo msbuild FormulaCodeGeneratorTask /p:Platform=%FormulaCodeGeneratorTaskPlatform% /p:Configuration=%PBuildConfiguration%
msbuild  ext\Formula\src\Extensions\FormulaCodeGeneratorTask\FormulaCodeGeneratorTask.csproj /p:Platform=%FormulaCodeGeneratorTaskPlatform% /p:Configuration=%PBuildConfiguration%


call :StartTimer

Bld\nuget restore -configfile NuGet.config P.sln

msbuild P.sln /p:Platform=%PBuildPlatform% /p:Configuration=%PBuildConfiguration% /p:SOLVER=NOSOLVER /fl
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
