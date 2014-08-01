echo "Combining all files"
..\..\..\Scripts\MergeFiles.exe -concate:MultiPaxos.p+properties.p+LeaderElection.p -out:temp.p
#echo "Merging all global functions"
#..\..\..\..\..\Scripts\MergeFiles.exe -merge:output.p -main:temp1.p -model:.\ModelFunctions_Implementation.p 
echo "Running P"
python.exe ..\..\..\Scripts\myRunAll.py .\temp temp.p