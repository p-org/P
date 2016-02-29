@echo off
setlocal
pushd %~dp0
cd ..

set Configuration=%1
if "%Configuration%"=="" set Configuration=Debug
set Platform=%2
if "%Platform%"=="" set Platform=x86

echo msbuild  P.sln /p:Platform=%Platform% /p:Configuration=%Configuration% /t:Clean
msbuild  P.sln /p:Platform=%Platform% /p:Configuration=%Configuration% /t:Clean

git submodule init
git submodule update
cd ext\zing

echo msbuild  Zing.sln /p:Platform=%Platform% /p:Configuration=Release
msbuild  Zing.sln /p:Platform=%Platform% /p:Configuration=Release
if ERRORLEVEL 1 goto :exit

cd ..\..

set BinariesFolder=Bld\Drops\%Configuration%\%Platform%\Binaries\

if NOT exist %BinariesFolder% mkdir %BinariesFolder%

for %%i in (Ext\Zing\zc\bin\%Platform%\Release\zc.exe
             Ext\Zing\ZingExplorer\bin\%Platform%\Release\ZingExplorer.dll
             Ext\Zing\Zinger\bin\%Platform%\Release\Zinger.exe
             Ext\Zing\Microsoft.Zing\bin\%Platform%\Release\Microsoft.Zing.dll
             Ext\Zing\Microsoft.Zing.Runtime\bin\%Platform%\Release\Microsoft.Zing.Runtime.dll
             Ext\Zing\Microsoft.Zing\bin\%Platform%\Release\Microsoft.Comega.dll
             Ext\Zing\Microsoft.Zing\bin\%Platform%\Release\Microsoft.Comega.Runtime.dll
             Ext\Zing\Resources\external\CCI\System.Compiler.dll
             Ext\Zing\Resources\external\CCI\System.Compiler.Framework.dll
             Ext\Zing\Resources\external\CCI\System.Compiler.Runtime.dll
             Ext\Zing\DelayingSchedulers\CustomDelayingScheduler\bin\%Platform%\Release\CustomDelayingScheduler.dll
             Ext\Zing\DelayingSchedulers\RandomDelayingScheduler\bin\%Platform%\Release\RandomDelayingScheduler.dll
             Ext\Zing\DelayingSchedulers\RoundRobinDelayingScheduler\bin\%Platform%\Release\RoundRobinDelayingScheduler.dll
             Ext\Zing\DelayingSchedulers\RunToCompletionDelayingScheduler\bin\%Platform%\Release\RunToCompletionDelayingScheduler.dll) do (
             
    echo copy %%i %BinariesFolder%
    copy %%i %BinariesFolder%
)


REM this code fixes a problem in MIDL compile by forcing recompile of these files for each configuration.
del Src\PrtDist\Core\NodeManager_c.c
del Src\PrtDist\Core\NodeManager_s.c

echo msbuild P.sln /p:Platform=%Platform% /p:Configuration=%Configuration%
msbuild  P.sln /p:Platform=%Platform% /p:Configuration=%Configuration%

:exit
popd