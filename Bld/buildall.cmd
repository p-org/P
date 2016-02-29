pushd %~dp0
call build.bat Debug x86 
call build.bat Release x86 
call build.bat Debug x64 
call build.bat Release x64 
popd