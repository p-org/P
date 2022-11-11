# PSym Commandline Options

PSym runtime provides a range of options to configure the model exploration.

Here is a summary of these options:

````
Commandline options for PSym
 -mode,--mode <Mode (string)>                   Mode of exploration: default, bmc, random, fuzz, dfs
 -tl,--time-limit <Time Limit (seconds)>        Time limit in seconds (default: 60). Use 0 for no
                                                limit.
 -ml,--memory-limit <Memory Limit (MB)>         Memory limit in megabytes (MB). Use 0 for no limit.
 -seed,--seed <Random Seed (integer)>           Random seed for the search (default: auto)
 -m,--method <Test Method (string)>             Name of the test method from where the symbolic
                                                engine should start exploration
 -p,--project <Project Name (string)>           Name of the project (default: auto)
 -o,--outdir <Output Dir (string)>              Name of the output directory (default: output)
 -replay,--replay <File Name (string)>          Name of the .schedule file with the counterexample
 -ms,--max-steps <Max Steps (integer)>          Max scheduling steps to be explored (default: 1000)
 -i,--iterations <Max Executions (integer)>     Number of schedules/executions to explore (default:
                                                no-limit)
 -sb,--sched-bound <Schedule Bound (integer)>   Max scheduling choice bound at each step during the
                                                search (default: 1)
 -db,--data-bound <Data Bound (integer)>        Max data choice bound at each step during the search
                                                (default: 1)
 -nsc,--no-state-caching                        Disable state caching
 -nb,--no-backtrack                             Disable stateful backtracking
 -corch,--choice-orch <Choice Orch. (string)>   Choice orchestration options: random, learn, none
                                                (default: random)
 -torch,--task-orch <Task Orch. (string)>       Task orchestration options: astar, learn, random,
                                                dfs (default: astar)
 -bpe,--backtracks-per-exe <(integer)>          Max number of backtracks to generate per execution
                                                (default: 2)
 -st,--solver <Solver Type (string)>            Solver type to use: bdd, yices2, z3, cvc5 (default:
                                                bdd)
 -et,--expr <Expression Type (string)>          Expression type to use: bdd, fraig, aig, native
                                                (default: bdd)
 -r,--read <File Name (string)>                 Name of the file with the program state
 -w,--write                                     Enable writing program state
 -nf,--no-filters                               Disable filter-based reductions
 -s,--stats <Collection Level (integer)>        Level of stats collection/reporting during the
                                                search (default: 1)
 -v,--verbose <Log Verbosity (integer)>         Level of verbosity in the log output (default: 0)
 -h,--help                                      Print the help message
````

For example, running:

````
    ./scripts/run_psym.sh Examples/tests/pingPong/ psymExample \
    -sb 2 -db 3 -tl 60 -ml 1024 -ms 5 -me 4
````
runs PSym with 
- at most 2 scheduling and 3 data choices explored in each step
- with a time limit of 60 seconds and memory limit of 1024 MB
- exploring up to 5 steps in depth
- and at most 4 symbolic executions
