@wcho off

for /f %%i in (failed-tests.txt) do (
  call :check %%i
)

goto :eof

:check
pushd %1
windiff acc_0.txt check-output.log
d:\tools\prompt /message "Do you want to copy this new baseline?"
if ERRORLEVEL 1 goto :nocopy
copy /y check-output.log acc_0.txt
:nocopy
popd
goto :eof
