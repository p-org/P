# Go to /Users/xashisk/ashish-ws/SyncedForkedRepo/P
cd ./Src/PRuntimes/PExplicitRuntime/
./scripts/build.sh
echo "-------------------Build Over---------------------------"
cd -
cd ../../scriptsRepo/src/P-Evaluation-Tests/
./scripts/run_pexplicitzshrc.sh ../../../SyncedForkedRepo/P/Tutorial/1_ClientServer test -tc tcMultipleClients --seed 0 -t 10 -s 0 -v 3 --schedules-per-task 2
cd -