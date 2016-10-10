@echo off

echo This program will show windiff of all failed tests and prompt you to update the baselines.

if NOT EXIST failed-tests.txt (
  echo Please run this tool from the TestResult* directory containing the 'failed-tests.txt'
  goto :eof
)

for /f %%i in (failed-tests.txt) do (
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