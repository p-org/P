# PSym Commandline Options

PSym runtime provides a range of options to configure the model exploration.

Here is a summary of these options:

````
 -corch,--choice-orchestration <Choice Orchestration (string)>     Choice orchestration options: rl,
                                                                   random, none (default: random)
 -db,--data-choice-bound <Data Choice Bound (integer)>             Max data choice bound at each
                                                                   step during the search (default:
                                                                   1)
 -et,--expr <Expression Type (string)>                             Expression type to use: bdd,
                                                                   fraig, aig, native (default: bdd)
 -h,--help                                                         Print the help message
 -m,--method <Test Method (string)>                                Name of the test method from
                                                                   where the symbolic engine should
                                                                   start exploration
 -me,--max-executions <Max Executions (integer)>                   Max number of executions to run
                                                                   (default: no-limit)
 -ml,--memory-limit <Memory Limit (MB)>                            Memory limit in megabytes (MB).
                                                                   Use 0 for no limit.
 -mode,--mode <Mode (string)>                                      Mode of exploration: default,
                                                                   bmc, random, fuzz
 -ms,--max-steps <Max Steps (integer)>                             Max steps/depth for the search
                                                                   (default: 1000)
 -mtpe,--max-tasks-per-execution <Tasks Per Execution (integer)>   Max number of backtrack tasks to
                                                                   generate per execution (default:
                                                                   2)
 -nb,--no-backtrack                                                Disable stateful backtracking
 -nf,--no-filters                                                  Disable filter-based reductions
 -nsc,--no-state-caching                                           Disable state caching
 -o,--output <Output Folder (string)>                              Name of the output folder
                                                                   (default: output)
 -p,--project <Project Name (string)>                              Name of the project (default:
                                                                   test)
 -r,--read <File Name (string)>                                    Name of the file with the program
                                                                   state
 -s,--stats <Collection Level (integer)>                           Level of stats collection during
                                                                   the search (default: 1)
 -sb,--sched-choice-bound <Schedule Choice Bound (integer)>        Max scheduling choice bound at
                                                                   each step during the search
                                                                   (default: 1)
 -seed,--seed <Random Seed (integer)>                              Random seed for the search
                                                                   (default: 0)
 -st,--solver <Solver Type (string)>                               Solver type to use: bdd, yices2,
                                                                   z3, cvc5 (default: bdd)
 -tl,--time-limit <Time Limit (seconds)>                           Time limit in seconds (default:
                                                                   60). Use 0 for no limit.
 -torch,--task-orchestration <Task Orchestration (string)>         Task orchestration options: rl,
                                                                   astar, random, dfs (default:
                                                                   astar)
 -v,--verbose <Log Verbosity (integer)>                            Level of verbosity for the
                                                                   logging (default: 1)
 -w,--write                                                        Enable writing program state
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
