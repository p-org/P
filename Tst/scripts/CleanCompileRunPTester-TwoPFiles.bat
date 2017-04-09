@echo off
REM this script only works for two .p files
REM it puts output.dll and output.pdb in fixed dir:
REM D:\PLanguage\P\Src\PTester\Regressions
REM arguments:
REM %1: first.p (the one with Main machine)
REM %2: test dir
REM %3: second.p
REM echo "start"
@echo on
del "%2\%1.4ml" "%2\%3.4ml" "%2\%1.cs" "%2\%3.cs" "%2\linker.c" "%2\linker.h" "%2\linker.dll" "%2\linker.pdb" "%2\linker.cs" "%2\%1.pdb" "%2\%1.dll"
pc.exe /generate:C# /outputDir:"%2" "%2\%1.p" "%2\%3.p"
pc.exe /link /outputDir:"%2" "%2\%1.4ml" /r:"%2\%3.4ml"
csc.exe "%2\%1.cs"  "%2\%3.cs" "%2\linker.cs" /debug /target:library /r:"D:\PLanguage\P\Bld\Drops\Debug\x86\Binaries\Prt.dll" /out:"%2\%1.dll"
pt.exe "%2\%1.dll"