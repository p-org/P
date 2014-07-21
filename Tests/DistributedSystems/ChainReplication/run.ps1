echo "Combining all files"
..\..\..\Scripts\MergeFiles.exe -concate:ChainReplication.p+properties.p+TestCase_Update-Query_Unblocking.p+ChainReplicationMaster.p -out:temp1.p
#echo "Merging all global functions"
#..\..\..\..\..\Scripts\MergeFiles.exe -merge:output.p -main:temp1.p -model:.\ModelFunctions_Implementation.p 
echo "Running P"
python.exe ..\..\..\Scripts\myRunAll.py .\temp temp1.p