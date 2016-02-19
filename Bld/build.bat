@echo off
setlocal
set bldexe=
pushd %~dp0
cd ..

set Configuration=%1
if "%Configuration%"=="" set Configuration=Debug
set Platform=%2
if "%Platform%"=="" set Platform=x86

if exist "%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe" (
  set bldexe="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
) else if exist "%WinDir%\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe" (
  set bldexe="%WinDir%\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe"
) else if exist "%WinDir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe" (
  set bldexe="%WinDir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe"
) else (
  echo Microsoft.NET framework version 4.0 or greater is required. Cannot build.
  echo Please install Visual Studio 2015 Community Edition
  popd
  exit /B 1
)


if exist "%ProgramFiles(x86)%\Git\cmd\git.exe" (
  set gitexe="%ProgramFiles(x86)%\Git\cmd\git.exe"
) else (
  echo Cannot find git.exe, please install it and ensure it is in your PATH.
  popd
  exit /B 1
)

%gitexe% submodule update --recursive
cd ext\zing

echo %bldexe% Zing.sln /p:Platform=%Platform% /p:Configuration=Release

%bldexe% Zing.sln /p:Platform=%Platform% /p:Configuration=Release
if ERRORLEVEL 1 goto :exit

if NOT exist %Platform% mkdir %Platform%

for %%i in (zc\bin\x86\Release\zc.exe
             ZingExplorer\bin\x86\Release\ZingExplorer.dll
             Zinger\bin\x86\Release\Zinger.exe
             Microsoft.Zing\bin\x86\Release\Microsoft.Zing.dll
             Microsoft.Zing.Runtime\bin\x86\Release\Microsoft.Zing.Runtime.dll
             Microsoft.Zing\bin\x86\Release\Microsoft.Comega.dll
             Microsoft.Zing\bin\x86\Release\Microsoft.Comega.Runtime.dll
             Resources\external\CCI\System.Compiler.dll
             Resources\external\CCI\System.Compiler.Framework.dll
             Resources\external\CCI\System.Compiler.Runtime.dll
             DelayingSchedulers\CustomDelayingScheduler\bin\x86\Release\CustomDelayingScheduler.dll
             DelayingSchedulers\RandomDelayingScheduler\bin\x86\Release\RandomDelayingScheduler.dll
             DelayingSchedulers\RoundRobinDelayingScheduler\bin\x86\Release\RoundRobinDelayingScheduler.dll
             DelayingSchedulers\RunToCompletionDelayingScheduler\bin\x86\Release\RunToCompletionDelayingScheduler.dll) do (
             
    copy %%i %Platform%
)
   
cd ..\..
REM this code fixes a problem in MIDL compile by forcing recompile of these files for each configuration.
del Src\PrtDist\Core\NodeManager_c.c
del Src\PrtDist\Core\NodeManager_s.c

echo %bldexe% P.sln /p:Platform=%Platform% /p:Configuration=%Configuration%
%bldexe% P.sln /p:Platform=%Platform% /p:Configuration=%Configuration%

:exit
popd