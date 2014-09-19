echo "ZC"
..\..\..\Ext\Zing\zc.exe /nowarning:292 .\temp\output.zing ..\..\..\Runtime\Zing\SMRuntime.zing
echo "Zinger"
..\..\..\Ext\Zing\Zinger.exe -s -p -sched:..\..\..\Ext\Zing\RunToCompletionDBSched.dll -eo -et:trace.txt .\temp\output.dll 

