@echo off
setlocal
set SCRIPTDIR=%~dp0
cd %SCRIPTDIR%
set PBuildConfiguration=Release
set PBuildPlatform=x86
set NoSync=true
set Clean=
set RunPArgs=
set NoBuild=

:parseargs
if /I "%1"=="/?" goto :help
if /I "%1"=="/h" goto :help
if /I "%1"=="/help" goto :help
if /I "%1"=="help" goto :help
if /I "%1"=="" goto :dobuild
if /I "%1"=="debug" set PBuildConfiguration=Debug&& goto :shift
if /I "%1"=="release" set PBuildConfiguration=Release&& goto :shift
if /I "%1"=="x86" set PBuildPlatform=x86&& goto :shift
if /I "%1"=="x64" set PBuildPlatform=x64&& goto :shift
if /I "%1"=="nosync" set NoSync=nosync&& goto :shift
if /I "%1"=="clean" set Clean=clean&& goto :shift
if /I "%1"=="noclean" set Clean=noclean&& goto :shift
if /I "%1"=="nobuild" set NoBuild=1&& goto :shift
set RunPArgs=%RunPArgs% %1
:shift
shift
goto :parseargs

:help
echo "Usage: testP [x86|x64] [debug|release] [nosync] [clean|noclean]"
goto :eof

:dobuild
if "%NoBuild%" == "1" goto :runtest

cd ..\Bld
call build.bat %PBuildConfiguration% %PBuildPlatform% %NoSync% %Clean%

:runtest
cd %SCRIPTDIR%

set RunPArgs=%RunPArgs% /Platform=%PBuildPlatform% /Configuration=%PBuildConfiguration%
echo %SCRIPTDIR%..\Bld\Drops\%PBuildConfiguration%\%PBuildPlatform%\Binaries\RunPTool.exe %RunPArgs%
"%SCRIPTDIR%..\Bld\Drops\%PBuildConfiguration%\%PBuildPlatform%\Binaries\RunPTool.exe" %RunPArgs%
set result=0
if %ERRORLEVEL% neq 0 (
  echo Tests failed.
  set result=1
) 

exit /B %result%


