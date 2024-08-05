# Go to /Users/xashisk/ashish-ws/SyncedForkedRepo/P
cd ./Src/PRuntimes/PExplicitRuntime/
./scripts/build.sh
echo "-------------------Build Over---------------------------"
cd -
cd ../../scriptsRepo/src/P-Evaluation-Tests/
echo "Running script ..."

#nproc 1 errors:
./scripts/run_pexplicitzshrc.sh /Users/xashisk/ashish-ws/SyncedForkedRepo/P/Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_8 test  --seed 0 -t 0 -s 0 -v 3 --schedules-per-task 2 --nproc 1 --no-backtrack
# ./scripts/run_pexplicitzshrc.sh /Users/xashisk/ashish-ws/SyncedForkedRepo/P/Tst/RegressionTests/Feature4DataTypes/DynamicError/CastInExprs3 test  --seed 0 -t 0 -s 0 -v 3 --schedules-per-task 2 --nproc 1 --no-backtrack
# ./scripts/run_pexplicitzshrc.sh /Users/xashisk/ashish-ws/SyncedForkedRepo/P/Tst/RegressionTests/Feature4DataTypes/DynamicError/CastInExprs4 test  --seed 0 -t 0 -s 0 -v 3 --schedules-per-task 2 --nproc 1 --no-backtrack
# ./scripts/run_pexplicitzshrc.sh /Users/xashisk/ashish-ws/SyncedForkedRepo/P/Tst/RegressionTests/Feature4DataTypes/DynamicError/CastInExprs5 test  --seed 0 -t 0 -s 0 -v 3 --schedules-per-task 2 --nproc 1 --no-backtrack
# ./scripts/run_pexplicitzshrc.sh /Users/xashisk/ashish-ws/SyncedForkedRepo/P/Tst/RegressionTests/Feature4DataTypes/DynamicError/CastInExprs6 test  --seed 0 -t 0 -s 0 -v 3 --schedules-per-task 2 --nproc 1 --no-backtrack
# ./scripts/run_pexplicitzshrc.sh /Users/xashisk/ashish-ws/SyncedForkedRepo/P/Tst/RegressionTests/Feature4DataTypes/DynamicError/EnumType1 test  --seed 0 -t 0 -s 0 -v 3 --schedules-per-task 2 --nproc 1 --no-backtrack
# ./scripts/run_pexplicitzshrc.sh /Users/xashisk/ashish-ws/SyncedForkedRepo/P/Tst/RegressionTests/Feature2Stmts/DynamicError/GotoStmt1 test  --seed 0 -t 0 -s 0 -v 3 --schedules-per-task 2 --nproc 1 --no-backtrack

#nproc 4 dynamic errors (80 failures (all in Dynamic Error folder)):
# expected: 2, got: 0 type errors (76/80):
# ./scripts/run_pexplicitzshrc.sh /Users/xashisk/ashish-ws/SyncedForkedRepo/P/Tst/RegressionTests/Feature1SMLevelDecls/DynamicError/AlonBug test  --seed 0 -t 0 -s 0 -v 3 --schedules-per-task 2 --nproc 4 --no-backtrack

# expected: 2, got: 5 type errors (4/80):
# ./scripts/run_pexplicitzshrc.sh /Users/xashisk/ashish-ws/SyncedForkedRepo/P/Tst/RegressionTests/Feature4DataTypes/DynamicError/CastInExprs3 test  --seed 0 -t 0 -s 0 -v 3 --schedules-per-task 2 --nproc 4 --no-backtrack



cd -

