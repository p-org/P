@echo off

set SRC_ROOT=%~dp0..

set MAINCLASS=com.runtimeverification.rvmonitor.java.rvj.Main

if "%1"=="-c" (
  SHIFT
  set MAINCLASS=com.runtimeverification.rvmonitor.c.rvc.Main
) 

set RELEASE=%SRC_ROOT%\lib

set PLUGINS=%RELEASE%\plugins
set LOGICPLUGINPATH=%PLUGINS%
set CP=%RELEASE%\*;%PLUGINS%\*
for /f %%a IN ('dir /b /s "%PLUGINS%\*.jar"') do call :concat %%a

java -cp "%CP%"  %MAINCLASS% %1 %2 %3 %4 %5 %6 %7 %8 %9
goto :eof

:concat
set CP=%CP%;%1
goto :eof
