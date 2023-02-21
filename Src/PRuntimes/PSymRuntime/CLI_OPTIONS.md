# PSym Commandline Options

PSym runtime provides a range of options to configure the model exploration.

Here is a summary of these options:

````
----------------------------
Commandline options for PSym
----------------------------
 -s,--strategy <Mode (string)>                  Exploration strategy: random, dfs, learn, symex
                                                (default: learn)
 -tc,--testcase <Test Case (string)>            Test case to explore
 -t,--timeout <Time Limit (seconds)>            Timeout in seconds (disabled by default)
 -m,--memout <Memory Limit (GB)>                Memory limit in Giga bytes (auto-detect by default)
 -o,--outdir <Output Dir (string)>              Dump output to directory (absolute or relative path)
 -v,--verbose <Log Verbosity (integer)>         Level of verbose log output during exploration
                                                (default: 0)
 -i,--iterations <Iterations (integer)>         Number of schedules to explore (default: 1)
 -ms,--max-steps <Max Steps (integer)>          Max scheduling steps to be explored (default: 1000)
 -fms,--fail-on-maxsteps                        Consider it a bug if the test hits the specified
                                                max-steps
 -nsc,--no-state-caching                        Disable state caching
 -corch,--choice-orch <Choice Orch. (string)>   Choice orchestration options: random, learn
                                                (default: learn)
 -torch,--task-orch <Task Orch. (string)>       Task orchestration options: astar, random, dfs,
                                                learn (default: learn)
 -sb,--sched-bound <Schedule Bound (integer)>   Max scheduling choice bound at each step during the
                                                search (default: 1)
 -db,--data-bound <Data Bound (integer)>        Max data choice bound at each step during the search
                                                (default: 1)
 -r,--replay <File Name (string)>               Schedule file to replay
    --seed <Random Seed (integer)>              Specify the random value generator seed
    --no-backtrack                              Disable stateful backtracking
    --backtracks-per-exe <(integer)>            Max number of backtracks to generate per execution
                                                (default: 2)
    --solver <Solver Type (string)>             Solver type to use: bdd, yices2, z3, cvc5 (default:
                                                bdd)
    --expr <Expression Type (string)>           Expression type to use: bdd, fraig, aig, native
                                                (default: bdd)
    --no-filters                                Disable filter-based reductions
    --read <File Name (string)>                 Name of the file with the program state
    --write                                     Enable writing program state
    --stats <Collection Level (integer)>        Level of stats collection/reporting during the
                                                search (default: 1)
    --config <File Name (string)>               Name of the JSON configuration file
 -h,--help                                      Show this help menu
````

For example, running:

````
    ./scripts/run_psym.sh Examples/tests/pingPong/ psymExample \
    --strategy learn --timeout 60 --memout 2 --iterations 4 --max-steps 5
````
runs PSym with 
- learning-based explicit-state search strategy
- with a time limit of 60 seconds and memory limit of 2 GB
- with at most 4 iterations
- and exploring up to 5 steps in depth
