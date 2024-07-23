# Go to /Users/xashisk/ashish-ws/SyncedForkedRepo/P
cd ./Src/PRuntimes/PExplicitRuntime/
./scripts/build.sh
echo "-------------------Build Over---------------------------"
cd -
cd ../../scriptsRepo/src/P-Evaluation-Tests/
echo "Running script ..."
./scripts/run_pexplicitzshrc.sh /Users/xashisk/ashish-ws/SyncedForkedRepo/P/Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_8 test  --seed 0 -t 10 -s 0 -v 3 --schedules-per-task 2 --nproc 1 --no-backtrack
# ./scripts/run_pexplicitzshrc.sh /Users/xashisk/ashish-ws/SyncedForkedRepo/P/Tst/RegressionTests/Feature2Stmts/DynamicError/GotoStmt1 test  --seed 0 -t 10 -s 0 -v 3 --schedules-per-task 2 --nproc 1 --no-backtrack
# ./scripts/run_pexplicitzshrc.sh /Users/xashisk/ashish-ws/SyncedForkedRepo/P/Tst/RegressionTests/Feature2Stmts/DynamicError/receive6 test  --seed 0 -t 10 -s 0 -v 3 --schedules-per-task 2 --nproc 1 --no-backtrack


cd -

