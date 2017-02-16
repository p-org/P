@echo off
REM this script only works for a single .p file
REM it puts output.dll and output.pdb in fixed dir:
REM D:\PLanguage\P\Src\PTester\Regressions
REM arguments:
REM %1: test name
REM %2: test dir
REM echo "start"
@echo on
del "%2\%1.4ml" "%2\%1.cs" "%2\linker.c" "%2\linker.h" "%2\output.dll" "%2\output.pdb" "%2\output.cs" "D:\PLanguage\P\Src\PTester\Regressions\%1.pdb" "D:\PLanguage\P\Src\PTester\Regressions\%1.dll"
del "D:\PLanguage\P\Src\PTester\Regressions\%1.dll"
pc.exe /generate:C#  /rebuild /outputDir:"%2" "%2\%1.p"
pc.exe /link /outputDir:"%2" "%2\%1.4ml"
csc.exe "%2\%1.cs" "%2\output.cs" /debug /target:library /r:"D:\PLanguage\P\Bld\Drops\Debug\x86\Binaries\Prt.dll" /out:"D:\PLanguage\P\Src\PTester\Regressions\%1.dll"
pt.exe "D:\PLanguage\P\Src\PTester\Regressions\%1.dll"