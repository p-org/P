@echo off
pushd %~dp0
for %%c in (Debug, Release) do (
  for %%p in (x86, x64) do (  
    echo ======================================================================
    echo calling build.bat %%c %%p 
    echo ======================================================================
    call build.bat %%c %%p 
    if ERRORLEVEL 1 goto :stop
  )
)
:stop
popd