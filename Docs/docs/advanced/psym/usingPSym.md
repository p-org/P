!!! check ""
    Before moving forward, we assume that you have successfully [installed PSym](install.md) :metal: .

In this section, we provide an overview of the steps involved in compiling and checking a P program with PSym
using the [client server](../../tutorial/clientserver.md) example in Tutorials.


??? info "Get the Client Server Example Locally"

    We will use the [ClientServer](https://github.com/p-org/P/tree/master/Tutorial/1_ClientServer) example from Tutorial folder in P repository to describe the process of compiling and checking a P program with PSym. Please clone the P repo and navigate to the ClientServer example in Tutorial.

    Clone P Repo locally:
    ```shell
    git clone https://github.com/p-org/P.git
    ```
    Navigate to the ClientServer examples folder:
    ```shell
    cd <P cloned folder>/Tutorial/1_ClientServer
    ```


### Compiling a P program for PSym


Simply pass the commandline argument `-generate:PSym` when running the P compiler `pc`.

??? info "Alternative way: Set `<Target>` as `PSym` in `*.pproj`"

    Set the `<Target>` field as `PSym` in the P project file (`*.pproj`)

    Example:

    ```xml hl_lines="10"
    <!-- P Project file for the Client Server example -->
    <Project>
    <ProjectName>ClientServer</ProjectName>
    <InputFiles>
       <PFile>./PSrc/</PFile>
       <PFile>./PSpec/</PFile>
       <PFile>./PTst/</PFile>
    </InputFiles>
    <OutputDir>./PGenerated/</OutputDir>
    <Target>PSym</Target>
    </Project>
    ```


!!! tip "Recommendation"

    We recommend using the P project file `*.pproj` along with passing `-generate:PSym` as commandline argument to the compiler
    to compile a P program for PSym.
    Commandline argument `-generate:XXX` takes priority over `<Target>YYY</Target>` in `*.pproj` file.


=== "Compiling the ClientServer project for PSym"

``` shell
pc -proj:ClientServer.pproj -generate:PSym
```

??? info "Expected Output"
    ```
    $ pc -proj:ClientServer.pproj -generate:PSym
    ----------------------------------------
    ==== Loading project file: ClientServer.pproj
    ....... includes p file: P/Tutorial/1_ClientServer/PSrc/Server.p
    ....... includes p file: P/Tutorial/1_ClientServer/PSrc/Client.p
    ....... includes p file: P/Tutorial/1_ClientServer/PSrc/AbstractBankServer.p
    ....... includes p file: P/Tutorial/1_ClientServer/PSrc/ClientServerModules.p
    ....... includes p file: P/Tutorial/1_ClientServer/PSpec/BankBalanceCorrect.p
    ....... includes p file: P/Tutorial/1_ClientServer/PTst/TestDriver.p
    ....... includes p file: P/Tutorial/1_ClientServer/PTst/Testscript.p
    ----------------------------------------
    ----------------------------------------
    Parsing ...
    Type checking ...
    Code generation ...
    Generated clientserver.java.
    ----------------------------------------
    Compiling ClientServer...
      ClientServer -> target/ClientServer-jar-with-dependencies.jar
    Build succeeded.
    ----------------------------------------
    ```

### Checking a P program with PSym

Compiling the ClientServer program generates a `ClientServer-jar-with-dependencies.jar`. This `.jar` is
the symbolically-instrumented intermediate representation of the P program in Java, along with a packaged PSym runtime.
Running this `.jar` file executes PSym to systematically explore behaviors of the program for the specified test case.

The `.jar` is present in the `target/` folder, also printed in the compiler output:
`ClientServer -> target/ClientServer-jar-with-dependencies.jar`

You can get the list of test cases defined in the P program by running the generated `.jar`:

```shell
java -jar target/ClientServer-jar-with-dependencies.jar
```

??? info "Expected Output"
    ```hl_lines="9 10 11 12"
    java -jar target/ClientServer-jar-with-dependencies.jar

    Picked up JAVA_TOOL_OPTIONS: -Dlog4j2.formatMsgNoLookups=true
    WARNING: sun.reflect.Reflection.getCallerClass is not supported. This will impact performance.
    Reflections took 100 ms to scan 1 urls, producing 38 keys and 168 values
    Loading:: /Volumes/workplace/psym/src/PSymTest/exp/Examples/Tutorial/1_ClientServer/target/ClientServer-jar-with-dependencies.jar
    Setting solver engine to BDD + Bdd
    Reflections took 63 ms to scan 1 urls, producing 38 keys and 168 values
    Provide /method or -m flag to qualify the test method name you wish to use.
    Possible options are::
    tcSingleClient
    tcMultipleClients
    ```

There are three test cases defined in the ClientServer P project, and you can specify which
test case to run by using the `-m` or `--method` parameter along with, say, a `--time-limit <seconds>` parameter to
specify a time limit for the run in seconds.
By default, PSym explores as many schedules as it can within a time limit of 60 seconds.

So to run the `tcSingleClient` test case for 10 seconds, we can use the following command:

```shell
java -jar target/ClientServer-jar-with-dependencies.jar \
    -m tcSingleClient \
    --time-limit 10
```

??? info "Expected Output"
    ``` hl_lines="9-10 12-13 15-17" linenums="1"
    Picked up JAVA_TOOL_OPTIONS: -Dlog4j2.formatMsgNoLookups=true
    WARNING: sun.reflect.Reflection.getCallerClass is not supported. This will impact performance.
    Reflections took 93 ms to scan 1 urls, producing 38 keys and 172 values
    . Checking /Users/goelaman/work/ws/version/github/P/Tutorial/1_ClientServer/target/ClientServer-jar-with-dependencies.jar
    Reflections took 45 ms to scan 1 urls, producing 38 keys and 172 values
    ... Method tcSingleClient
    ... Project clientserver is using 'default' strategy (seed:0)
    --------------------
      Time       Memory        Coverage      Schedule         Remaining          Depth      States
    00:00:10     0.2 GB     1.4246418994 %      140        1820 (100 % data)        1         8043
    --------------------
    Estimated Coverage:: 1.4246418994 %
    Distinct States Explored:: 8043
    --------------------
    Explored 140 single executions
    Took 10 seconds and 0.2 GB
    Result: partially safe with 1820 backtracks remaining
    --------------------
    java.lang.Exception: TIMEOUT
    at psym.commandline.EntryPoint.process(EntryPoint.java:97)
    at psym.commandline.EntryPoint.run(EntryPoint.java:138)
    at psym.commandline.PSym.main(PSym.java:73)
    ```

    Here, PSym explores 140 schedules, checking 8043 distinct states, and achieving an estimated coverage of ~ 1.4 %.

    Check file `output/coverage-clientserver.log` for a detailed Coverage Report.

    Check file `output/stats-clientserver.log` for a detailed Statistics Report.


### Coverage

At the end of a run, PSym reports a coverage metric as an estimated percentage of the execution tree that is explored during
the run. Assuming a uniform probability for each scheduling/data choice, this metric reports the probability of a
randomly-sampled schedule to be bug free.

??? info "Continuous Feedback"

    During the run, PSym prints useful metrics that summarizes the current status of the run.

    For example, for the ClientServer run above, PSym prints:

    ```
      Time       Memory        Coverage      Schedule         Remaining          Depth      States
    00:00:10     0.2 GB     1.4246418994 %      140        1820 (100 % data)        1         8043
    ```

    that summarizes:

    | Label     | Description                                                                      |
    |-----------|----------------------------------------------------------------------------------|
    | Time      | Elapsed runtime in ``hh:mm:ss`` format                                           |
    | Memory    | Memory usage in gigabytes                                                        |
    | Coverage  | Estimated Coverage                                                               |
    | Schedule  | Schedule number                                                                  |
    | Remaining | Number of unexplored backtracks/choices remaining (as well as % of data choices) |
    | Depth     | Current depth of the exploration                                                 |
    | States    | Number of distinct states explored                                               |


!!! danger "[Important] Is the coverage reported exactly measures the % of state-space covered?"

    Sadly, No :pensive:!

    PSym's coverage metric is **not** a perfect state-space coverage metric.
    This metric gives more weightage to shorter schedules, or more precisely, schedules with fewer schedule/data
    choices at shallower search depths.
    Therefore, a PSym run can quickly reach a high estimated coverage (> 99 %) due to exploring shorter schedules
    first, after which gaining the remaining left-over percentage can become increasingly (and exponentially) difficult.

    Our recommendation is to target achieving coverage up to 11 nines, i.e., 99.999999999 % to be sufficiently confident
    of the absence of bug.
    Additionally, check PSym's Coverage Report (`` output/coverage-*.log ``) to understand the state-space covered
    during the run, as well as the number of distinct states explored.


At the end of a run, PSym also prints a coverage report in `` output/coverage-*.log `` that tabulates, for each
exploration step/depth, the number of schedule/data choices explored during the run, along with the number of
choices remaining as unexplored backtracks.
For example, coverage report corresponding to the previous ClientServer run can be found in `` output/coverage-clientserver.log ``


??? example "Example"

    Here is a snippet of the ClientServer coverage report: `` output/coverage-clientserver.log ``

    ```
    -----------------
    Coverage Report::
    -----------------
    Covered choices:    8239 scheduling,  1962 data
    Remaining choices:     0 scheduling, 30292 data
    -------------------------------------
    Depth   Covered       Remaining
          sch    data     sch    data
    -------------------------------------
     0            100
     1    100
     2    100     134            5806
     3    135
     4    134
     5    134
     6    134
     7    134      50            1162
     8    134      77            2659
     9    127
    10    127
    11    127      36             559
    12    127      48            1045
    13    124      36            1149
    14    120
    15    120      23             296
    16    120      36             621
    17    114      34             766
    18    111      18             468
    19    111      19             203
    20    112      24             374
    21    109      31             582
    22    106      25             494
    23    106      20             250
    24    104      19             256
    25    102      22             326
    26     98      26             365
    27     97      23             319
    28     96      19             222
    29     95      20             268
    30     95      23             298
    31     93      23             279
    32     91      19             233
    33     91      20             243
    34     89      18             232
    35     88      22             263
    36     85      17             181
    37     83      15             176
    38     79      16             198
    39     76      19             221
    40     73      20             199
    41     73      15             176
    42     71      10             123
    43     70      20             225
    44     68      20             208
    45     68      16             187
    46     66      10             123
    47     66      17             190
    48     66      18             187
    49     64      16             186
    50     62      10             114
                .
                .
                .
    ```

??? tip "Improving Coverage for ClientServer"
    Looks like the ClientServer example is quite data heavy, since there are lots of unexplored data choices at different steps
    (check the rightmost column of `` output/coverage-clientserver.log ``).
    A good idea is to reduce the amount of data non-determinism by reducing the number of choices in the
    `choose(*)` expressions, such as the number of data choices in setting the initial bank balances in expression
    `choose(100)` [here](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PTst/TestDriver.p#L30).


??? info "Remaining choices in Coverage Report"

    Note that the coverage report can be incomplete, since it tabulates the remaining choices as of by the end of a run.
    Each remaining choice, say at a step N, when explored by PSym can discover many more choices at steps >= N+1.


### CLI Options

Here is a list of frequently-used commandline options that can be passed to the `.jar`:

| CLI Option                 | Description                                                                   |    Default    |
|----------------------------|-------------------------------------------------------------------------------|:-------------:|
| `` --method <string> ``    | Name of the test method to execute                                            |  `` auto ``   |
| `` --time-limit <sec> ``   | Time limit in seconds (use 0 for no limit)                                    |   `` 60 ``    |
| `` --memory-limit <MB> ``  | Memory limit in megabytes (use 0 for no limit)                                |  `` auto ``   |
| `` --schedules <int> ``    | Number of schedules to explore (use 0 for no limit)                           |    `` 0 ``    |
| `` --max-steps <int> ``    | Max scheduling steps to be explored per schedule                              |  `` 1000 ``   |
| `` --seed <int> ``         | Random seed to use for the exploration                                        |    `` 0 ``    |
| `` --verbose <int> ``      | Level of verbosity in the log output                                          |    `` 0 ``    |
| `` --mode <string> ``      | Preconfigured exploration mode to use (`` default ``, `` bmc ``, ``  fuzz ``) | `` default `` |

For a complete list of options, pass the argument ` --help `.

??? tip "Exploration Techniques"
    PSym implements a collection of configurable techniques summarized as follows:

    | Technique             | Description                                                                         |
    |-----------------------|-------------------------------------------------------------------------------------|
    | Search Strategy       | Configure the order in which search is performed: `astar`, `random`, `dfs`, `learn` |
    | Choice Selection      | Configure how a scheduling or data choice is selected: `random`, `learn`            |
    | Never Repeat States   | Track distinct states to avoid state revisits                                       |
    | Stateful Backtracking | Backtrack directly without replay                                                   |
    | BMC                   | Run PSym as a bounded model checker                                                 |


!!! info "Preconfigured Modes"

    For ease of usage, PSym provides a set of preconfigured exploration modes as follows:

    | Mode      | Description                                                                                                                                                                                                                   |
    |-----------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
    | `default` | Explore single execution at a time <br/> Search Strategy = `astar` <br/> Choice Selection = `random` <br/> Never Repeat States = `ON` <br/> Stateful Backtracking = `ON` <br/> BMC = `OFF`                                    |
    | `bmc`     | Explore all executions together symbolically as a bounded model checker <br/> Search Strategy = `N/A` <br/> Choice Selection = `N/A` <br/> Never Repeat States = `OFF` <br/> Stateful Backtracking = `N/A` <br/> BMC = `ON`   |
    | `fuzz`    | Explore like a random fuzzer (but never repeat an execution!) <br/> Search Strategy = `random` <br/> Choice Selection = `random` <br/> Never Repeat States = `OFF` <br/> Stateful Backtracking = `OFF` <br/> BMC = `OFF`      |
    | `dfs`     | Explore single executions at a time in depth-first manner <br/> Search Strategy = `dfs` <br/> Choice Selection = `random` <br/> Never Repeat States = `ON` <br/> Stateful Backtracking = `ON` <br/> BMC = `OFF`               |
    | `learn`   | Explore single execution at a time with learning <br/> Search Strategy = `learn` <br/> Choice Selection = `learn` <br/> Never Repeat States = `ON` <br/> Stateful Backtracking = `ON` <br/> BMC = `OFF`                       |

    Pass the CLI argument ` --mode <option> ` to set the exploration mode.


You are now a pro :drum:! Give PSym a try on your P model and shared your feedback with us.