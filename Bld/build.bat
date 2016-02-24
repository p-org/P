@echo off
setlocal
pushd %~dp0
cd ..

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

echo msbuild P.sln /p:Platform=%Platform% /p:Configuration=%Configuration%
msbuild  P.sln /p:Platform=%Platform% /p:Configuration=%Configuration%

:exit
popd