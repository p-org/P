echo "Combining all files"
..\..\..\Scripts\MergeFiles.exe -concate:ChainReplication.p+LivenessProperties.p+Properties.p+TestCase_Update-Query_Nodes=2.p+ChainReplicationMaster.p -out:temp1.p
#echo "Merging all global functions"
#..\..\..\..\..\Scripts\MergeFiles.exe -merge:output.p -main:temp1.p -model:.\ModelFunctions_Implementation.p 
echo "Running P"
python.exe ..\..\..\Scripts\runAllTests_liveness.py .\temp temp1.p