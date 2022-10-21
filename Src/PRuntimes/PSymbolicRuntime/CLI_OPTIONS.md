# PSym Commandline Options

PSym runtime provides a range of options to configure the model exploration.

Here is a summary of these options:

````
[Configure Orchestration]
    -orch,--orchestration <Orchestration Mode (string)>               Orchestration options: coverage-astar, 
                                                                      coverage-estimate, random, dfs (default: coverage-astar)                                                                     
    -orchmt,--orchestration-max-tasks <Max Backtrack Tasks (integer)> Max number of backtrack tasks
                                                                      to generate per execution (default: 2)

[Configure Symbolic Width]
    -sb,--sched-choice-bound <Max Schedule Choice Bound (integer)>   Max scheduling choice bound at
                                                                     each depth during the search (default: 1)
    -cb,--choice-bound <Max Choice Bound (integer)>                  Max data choice bound at each depth
                                                                     during the search (default: 1)

[Configure Resource Limits]
    -tl,--time-limit <Time Limit (seconds)>                          Time limit in seconds (default: 60).
                                                                     Use 0 for no limit.
    -ml,--memory-limit <Memory Limit (MB)>                           Memory limit in megabytes (MB).
                                                                     Use 0 for no limit.

[Configure Test Method]
    -m,--method <Name of Test Method (string)>                       Name of the test method from where
                                                                     the symbolic engine should start
                                                                     exploration

[Configure Search Depth]
    -ms,--max-steps <Max Steps (integer)>                            Max scheduling steps for the
                                                                     search (default: 1000)

[Configure Search Iterations]
    -me,--max-executions <Max Executions (integer)>                  Max number of executions to run (default: no-limit)


[Configure Solver Backend]
    -st,--solver <Solver Type (string)>                              Solver type to use: bdd, monosat,
                                                                     yices2, z3, cvc5 (default: bdd)
    -et,--expr <Expression Type (string)>                            Expression type to use: bdd,
                                                                     fraig, aig, native (default: bdd)

[Configure Output/Project Name]
    -o,--output <Output Folder (string)>                             Name of the output folder (default: output)
    -p,--project <Project Name (string)>                             Name of the project (default: test)

[Configure Reading/Writing Backtracks]
    -r,--read <File Name (string)>                                   Name of the file with the program
                                                                     backtrack state
    -w,--write                                                       Enable writing program state

[Configure Backtracks, Filtering, Queue Semantics]
    -nb,--no-backtrack                                               Disable stateful backtracking
    -nf,--no-filters                                                 Disable filter-based reductions
    -rq,--receiver-queue                                             Disable sender queue reduction to
                                                                     get receiver queue semantics

[Configure State Caching]
    -nsc,--no-state-caching                                          Disable state caching

[Configure Randomness]
    -nr,--no-random                                                  Disable randomization
    -seed,--seed <Random Seed (integer)>                             Random seed for the search (default: 0)

[Configure Statistics and Logs]
    -s,--stats <Collection Level>                                    Level of stats collection during
                                                                     the search (default: 1)
    -v,--verbose <Log Verbosity>                                     Level of verbosity for the logging
    -h,--help                                                        Print this help message (default: 1)
````

For example, running:

````
    ./scripts/run_psym.sh Examples/tests/pingPong/pingPongNew/ psymExample \
    -sb 2 -cb 3 -tl 60 -ml 1024 -ms 5 -me 4
````
runs PSym with 
- at most 2 scheduling and 3 data choices explored in each step
- with a time limit of 60 seconds and memory limit of 1024 MB
- exploring at most 5 steps in depth
- and at most 4 symbolic executions
