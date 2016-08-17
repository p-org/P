@echo off
setlocal
set SCRIPTDIR=%~dp0
cd %SCRIPTDIR%
set Configuration=Release
set Platform=x86
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
if /I "%1"=="debug" set Configuration=Debug&& goto :shift
if /I "%1"=="release" set Configuration=Release&& goto :shift
if /I "%1"=="x86" set Platform=x86&& goto :shift
if /I "%1"=="x64" set Platform=x64&& goto :shift
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
call build.bat %Configuration% %Platform% %NoSync% %Clean%

:runtest
cd %SCRIPTDIR%

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

echo msbuild  %SCRIPTDIR%\Tools\RunPTool\RunPTool.csproj /t:Rebuild /p:Configuration=%Configuration% /p:Platform=%Platform% /nologo
msbuild  %SCRIPTDIR%\Tools\RunPTool\RunPTool.csproj /t:Rebuild /p:Configuration=%Configuration% /p:Platform=%Platform%  /nologo
if %ERRORLEVEL% neq 0 (
  echo Could not compile test utility.
  exit /B 1
)

set RunPArgs=%RunPArgs% /Platform=%Platform% /Configuration=%Configuration%
echo %SCRIPTDIR%..\Bld\Drops\%Configuration%\%Platform%\Binaries\RunPTool.exe %RunPArgs%
"%SCRIPTDIR%..\Bld\Drops\%Configuration%\%Platform%\Binaries\RunPTool.exe" %RunPArgs%
if %ERRORLEVEL% neq 0 (
  echo Tests failed.
  exit /B 1
) else ( exit /B 0 )
