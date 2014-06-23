echo "Combining all files"
..\..\..\Scripts\MergeFiles.exe -concate:PingPong.p+NetworkLayer.p+NodeManager.p+CentralServer.p -out:temp1.p
echo "Merging all global functions"
..\..\..\Scripts\MergeFiles.exe -merge:output.p -main:temp1.p -model:.\ModelFunctions_Implementation.p 
echo "Running P"
python.exe ..\..\..\Scripts\myRunAll.py .\temp output.p