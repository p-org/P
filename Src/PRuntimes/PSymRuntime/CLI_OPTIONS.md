# PSym Commandline Options

PSym runtime provides a range of options to configure the model exploration.

Here is a summary of these options:

````
usage: java -jar <.jar-file> [options]
-----------------------------------
Commandline options for PSym/PCover
-----------------------------------
 -tc,--testcase <Test Case (string)>          Test case to explore
 -pn,--projname <Project Name (string)>       Project name
 -o,--outdir <Output Dir (string)>            Dump output to directory (absolute or relative path)
 -t,--timeout <Time Limit (seconds)>          Timeout in seconds (disabled by default)
 -m,--memout <Memory Limit (GB)>              Memory limit in Giga bytes (auto-detect by default)
 -v,--verbose <Log Verbosity (integer)>       Level of verbose log output during exploration
                                              (default: 0)
 -st,--strategy <Strategy (string)>           Exploration strategy: symbolic, random, dfs, learn,
                                              stateless (default: symbolic)
 -s,--schedules <Schedules (integer)>         Number of schedules to explore (default: 1)
 -ms,--max-steps <Max Steps (integer)>        Max scheduling steps to be explored (default: 10,000)
 -fms,--fail-on-maxsteps                      Consider it a bug if the test hits the specified
                                              max-steps
 -r,--replay <File Name (string)>             Schedule file to replay
    --seed <Random Seed (integer)>            Specify the random value generator seed
 -sb,--sch-bound <Schedule Bound (integer)>   Max scheduling choice bound at each step during the
                                              search (default: unbounded)
 -db,--data-bound <Data Bound (integer)>      Max data choice bound at each step during the search
                                              (default: unbounded)
    --config <File Name (string)>             Name of the JSON configuration file
 -h,--help                                    Show this help menu
See https://p-org.github.io/P/ for details.
````

For example, running:

````
    ./scripts/run_psym.sh Examples/tests/pingPong/ psymExample \
    --strategy learn --timeout 60 --memout 2 --schedules 4 --max-steps 5
````
runs PSym with
- learning-based explicit-state search strategy
- with a time limit of 60 seconds and memory limit of 2 GB
- with at most 4 schedules
- and exploring up to 5 steps in depth
