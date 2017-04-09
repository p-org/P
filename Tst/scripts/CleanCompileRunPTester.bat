@echo off
REM this script only works for a single .p file
REM it puts output.dll and output.pdb in fixed dir:
REM D:\PLanguage\P\Src\PTester\Regressions
REM arguments:
REM %1: test dir
REM %2: test name
REM echo "start"
@echo on
del "%1\%2.4ml" "%1\%2.cs" "%1\linker.c" "%1\linker.h" "%1\linker.dll" "%1\linker.pdb" "%1\linker.cs" "%1\%2.pdb" "%1\%2.dll"
pc.exe /generate:C# /outputDir:"%1" "%1\%2.p"
pc.exe /link /outputDir:"%1" /r:"%1\%2.4ml"
csc.exe "%1\%2.cs" "%1\linker.cs" /debug /target:library /r:"D:\PLanguage\P\Bld\Drops\Debug\x86\Binaries\Prt.dll" /out:"%1\%2.dll"
pt.exe "%1\%2.dll"