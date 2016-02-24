echo off
setlocal
set SCRIPTDIR=%~dp0
cd %SCRIPTDIR%
cd ..\Bld
REM call build.bat 
cd %SCRIPTDIR%

echo msbuild  %SCRIPTDIR%\Tools\RunPTool\RunPTool.csproj /t:Rebuild /p:Configuration=Debug /p:Platform=x86 /nologo
msbuild  %SCRIPTDIR%\Tools\RunPTool\RunPTool.csproj /t:Rebuild /p:Configuration=Debug /p:Platform=x86  /nologo
if %ERRORLEVEL% neq 0 (
  echo Could not compile test utility.
  exit /B 1
)

echo %SCRIPTDIR%..\Bld\Drops\Debug\x86\Binaries\RunPTool.exe %1 %2 %3
"%SCRIPTDIR%..\Bld\Drops\Debug\x86\Binaries\RunPTool.exe" %1 %2 %3
if %ERRORLEVEL% neq 0 (
  echo Tests failed.
  exit /B 1
) else ( exit /B 0 )