@echo off

for /f %%i in (TestResult\failed-tests.txt) do (
  call :check %%i
)

goto :eof

:check
echo %1
pushd %1
diff acc_0.txt check-output.log 
if ERRORLEVEL 1 goto :windiff
goto :nocopy
:windiff
windiff acc_0.txt check-output.log
d:\tools\prompt /message "Do you want to copy this new baseline?"
if ERRORLEVEL 1 goto :nocopy
copy /y check-output.log acc_0.txt
:nocopy
popd
goto :eof