echo off
set bldexe = ""

if exist "%WinDir%\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe" (
  set bldexe="%WinDir%\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe"
) else if exist "%WinDir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe" (
  set bldexe="%WinDir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe"
) else (
  echo Microsoft.NET framework version 4.0 or greater is required. Cannot build.
  exit /B 1)

echo Bootstrapping the build utility...
%bldexe%  .\PlangBuild\PlangBuild\PlangBuild.csproj /t:Clean /p:Configuration=Debug /p:Platform=AnyCPU /verbosity:quiet /nologo
if %ERRORLEVEL% neq 0 (
  echo Could not clean build utility.
  exit /B 1
)

%bldexe%  .\PlangBuild\PlangBuild\PlangBuild.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /verbosity:quiet /nologo
if %ERRORLEVEL% neq 0 (
  echo Could not compile build utility.
  exit /B 1
)

.\PlangBuild\PlangBuild\bin\Debug\PlangBuild.exe %1 %2
if %ERRORLEVEL% neq 0 (
  echo Build failed.
  exit /B 1
) else ( exit /B 0 )