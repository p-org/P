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

??? info "Alternative way"
    Set the `<Target>` field as `PSym` in the P project file (`*.pproj`)

    ??? example "Example"
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

??? info "Expected Output (scroll to the bottom)"
    ``` hl_lines="424-425 427-429"
    Picked up JAVA_TOOL_OPTIONS: -Dlog4j2.formatMsgNoLookups=true
    WARNING: sun.reflect.Reflection.getCallerClass is not supported. This will impact performance.
    Reflections took 86 ms to scan 1 urls, producing 38 keys and 172 values
    Loading:: /Users/goelaman/work/ws/version/github/P/Tutorial/1_ClientServer/target/ClientServer-jar-with-dependencies.jar
    Reflections took 58 ms to scan 1 urls, producing 38 keys and 172 values
    Setting Test Driver:: tcSingleClient
    --------------------
    Starting Iteration: 1 from Step: 0
    Execution finished in 111 steps
    --------------------
    Starting Iteration: 2 from Step: 1
    Execution finished in 36 steps
    --------------------
    Starting Iteration: 3 from Step: 1
    Execution finished in 49 steps
    --------------------
    Starting Iteration: 4 from Step: 1
    Execution finished in 41 steps
    --------------------
    Starting Iteration: 5 from Step: 1
    Execution finished in 193 steps
    --------------------
    Starting Iteration: 6 from Step: 1
    Execution finished in 31 steps
    --------------------
    Starting Iteration: 7 from Step: 1
    Execution finished in 14 steps
    --------------------
    Starting Iteration: 8 from Step: 1
    Execution finished in 190 steps
    --------------------
    Starting Iteration: 9 from Step: 1
    Execution finished in 26 steps
    --------------------
    Starting Iteration: 10 from Step: 1
    Execution finished in 130 steps
    --------------------
    Starting Iteration: 11 from Step: 1
    Execution finished in 44 steps
    --------------------
    Starting Iteration: 12 from Step: 1
    Execution finished in 59 steps
    --------------------
    Starting Iteration: 13 from Step: 1
    Execution finished in 144 steps
    --------------------
    Starting Iteration: 14 from Step: 1
    Execution finished in 74 steps
    --------------------
    Starting Iteration: 15 from Step: 1
    Execution finished in 42 steps
    --------------------
    Starting Iteration: 16 from Step: 1
    Execution finished in 79 steps
    --------------------
    Starting Iteration: 17 from Step: 1
    Execution finished in 9 steps
    --------------------
    Starting Iteration: 18 from Step: 1
    Execution finished in 18 steps
    --------------------
    Starting Iteration: 19 from Step: 1
    Execution finished in 31 steps
    --------------------
    Starting Iteration: 20 from Step: 1
    Execution finished in 37 steps
    --------------------
    Starting Iteration: 21 from Step: 1
    Execution finished in 42 steps
    --------------------
    Starting Iteration: 22 from Step: 1
    Execution finished in 22 steps
    --------------------
    Starting Iteration: 23 from Step: 1
    Execution finished in 64 steps
    --------------------
    Starting Iteration: 24 from Step: 1
    Execution finished in 18 steps
    --------------------
    Starting Iteration: 25 from Step: 1
    Execution finished in 179 steps
    --------------------
    Starting Iteration: 26 from Step: 1
    Execution finished in 82 steps
    --------------------
    Starting Iteration: 27 from Step: 1
    Execution finished in 236 steps
    --------------------
    Starting Iteration: 28 from Step: 1
    Execution finished in 201 steps
    --------------------
    Starting Iteration: 29 from Step: 1
    Execution finished in 4 steps
    --------------------
    Starting Iteration: 30 from Step: 1
    Execution finished in 83 steps
    --------------------
    Starting Iteration: 31 from Step: 1
    Execution finished in 40 steps
    --------------------
    Starting Iteration: 32 from Step: 1
    Execution finished in 170 steps
    --------------------
    Starting Iteration: 33 from Step: 1
    Execution finished in 50 steps
    --------------------
    Starting Iteration: 34 from Step: 1
    Execution finished in 61 steps
    --------------------
    Starting Iteration: 35 from Step: 1
    Execution finished in 34 steps
    --------------------
    Starting Iteration: 36 from Step: 1
    Execution finished in 59 steps
    --------------------
    Starting Iteration: 37 from Step: 1
    Execution finished in 56 steps
    --------------------
    Starting Iteration: 38 from Step: 1
    Execution finished in 38 steps
    --------------------
    Starting Iteration: 39 from Step: 1
    Execution finished in 32 steps
    --------------------
    Starting Iteration: 40 from Step: 1
    Execution finished in 26 steps
    --------------------
    Starting Iteration: 41 from Step: 1
    Execution finished in 88 steps
    --------------------
    Starting Iteration: 42 from Step: 1
    Execution finished in 46 steps
    --------------------
    Starting Iteration: 43 from Step: 1
    Execution finished in 61 steps
    --------------------
    Starting Iteration: 44 from Step: 1
    Execution finished in 78 steps
    --------------------
    Starting Iteration: 45 from Step: 1
    Execution finished in 73 steps
    --------------------
    Starting Iteration: 46 from Step: 1
    Execution finished in 39 steps
    --------------------
    Starting Iteration: 47 from Step: 1
    Execution finished in 14 steps
    --------------------
    Starting Iteration: 48 from Step: 1
    Execution finished in 53 steps
    --------------------
    Starting Iteration: 49 from Step: 1
    Execution finished in 138 steps
    --------------------
    Starting Iteration: 50 from Step: 1
    Execution finished in 65 steps
    --------------------
    Starting Iteration: 51 from Step: 1
    Execution finished in 126 steps
    --------------------
    Starting Iteration: 52 from Step: 1
    Execution finished in 18 steps
    --------------------
    Starting Iteration: 53 from Step: 1
    Execution finished in 106 steps
    --------------------
    Starting Iteration: 54 from Step: 1
    Execution finished in 38 steps
    --------------------
    Starting Iteration: 55 from Step: 1
    Execution finished in 24 steps
    --------------------
    Starting Iteration: 56 from Step: 1
    Execution finished in 97 steps
    --------------------
    Starting Iteration: 57 from Step: 1
    Execution finished in 64 steps
    --------------------
    Starting Iteration: 58 from Step: 1
    Execution finished in 9 steps
    --------------------
    Starting Iteration: 59 from Step: 1
    Execution finished in 36 steps
    --------------------
    Starting Iteration: 60 from Step: 1
    Execution finished in 72 steps
    --------------------
    Starting Iteration: 61 from Step: 1
    Execution finished in 40 steps
    --------------------
    Starting Iteration: 62 from Step: 1
    Execution finished in 46 steps
    --------------------
    Starting Iteration: 63 from Step: 1
    Execution finished in 69 steps
    --------------------
    Starting Iteration: 64 from Step: 1
    Execution finished in 77 steps
    --------------------
    Starting Iteration: 65 from Step: 1
    Execution finished in 64 steps
    --------------------
    Starting Iteration: 66 from Step: 1
    Execution finished in 9 steps
    --------------------
    Starting Iteration: 67 from Step: 1
    Execution finished in 439 steps
    --------------------
    Starting Iteration: 68 from Step: 1
    Execution finished in 43 steps
    --------------------
    Starting Iteration: 69 from Step: 1
    Execution finished in 162 steps
    --------------------
    Starting Iteration: 70 from Step: 1
    Execution finished in 38 steps
    --------------------
    Starting Iteration: 71 from Step: 1
    Execution finished in 53 steps
    --------------------
    Starting Iteration: 72 from Step: 1
    Execution finished in 28 steps
    --------------------
    Starting Iteration: 73 from Step: 1
    Execution finished in 32 steps
    --------------------
    Starting Iteration: 74 from Step: 1
    Execution finished in 27 steps
    --------------------
    Starting Iteration: 75 from Step: 1
    Execution finished in 44 steps
    --------------------
    Starting Iteration: 76 from Step: 1
    Execution finished in 25 steps
    --------------------
    Starting Iteration: 77 from Step: 1
    Execution finished in 24 steps
    --------------------
    Starting Iteration: 78 from Step: 1
    Execution finished in 40 steps
    --------------------
    Starting Iteration: 79 from Step: 1
    Execution finished in 22 steps
    --------------------
    Starting Iteration: 80 from Step: 1
    Execution finished in 118 steps
    --------------------
    Starting Iteration: 81 from Step: 1
    Execution finished in 60 steps
    --------------------
    Starting Iteration: 82 from Step: 1
    Execution finished in 39 steps
    --------------------
    Starting Iteration: 83 from Step: 1
    Execution finished in 112 steps
    --------------------
    Starting Iteration: 84 from Step: 1
    Execution finished in 56 steps
    --------------------
    Starting Iteration: 85 from Step: 1
    Execution finished in 35 steps
    --------------------
    Starting Iteration: 86 from Step: 1
    Execution finished in 62 steps
    --------------------
    Starting Iteration: 87 from Step: 1
    Execution finished in 36 steps
    --------------------
    Starting Iteration: 88 from Step: 1
    Execution finished in 92 steps
    --------------------
    Starting Iteration: 89 from Step: 1
    Execution finished in 66 steps
    --------------------
    Starting Iteration: 90 from Step: 1
    Execution finished in 26 steps
    --------------------
    Starting Iteration: 91 from Step: 1
    Execution finished in 34 steps
    --------------------
    Starting Iteration: 92 from Step: 1
    Execution finished in 184 steps
    --------------------
    Starting Iteration: 93 from Step: 1
    Execution finished in 128 steps
    --------------------
    Starting Iteration: 94 from Step: 1
    Execution finished in 14 steps
    --------------------
    Starting Iteration: 95 from Step: 1
    Execution finished in 108 steps
    --------------------
    Starting Iteration: 96 from Step: 1
    Execution finished in 39 steps
    --------------------
    Starting Iteration: 97 from Step: 1
    Execution finished in 82 steps
    --------------------
    Starting Iteration: 98 from Step: 1
    Execution finished in 9 steps
    --------------------
    Starting Iteration: 99 from Step: 1
    Execution finished in 134 steps
    --------------------
    Starting Iteration: 100 from Step: 1
    Execution finished in 26 steps
    --------------------
    Starting Iteration: 101 from Step: 63
    Execution finished in 81 steps
    --------------------
    Starting Iteration: 102 from Step: 1
    Execution finished in 21 steps
    --------------------
    Starting Iteration: 103 from Step: 1
    Execution finished in 9 steps
    --------------------
    Starting Iteration: 104 from Step: 1
    Execution finished in 49 steps
    --------------------
    Starting Iteration: 105 from Step: 1
    Execution finished in 53 steps
    --------------------
    Starting Iteration: 106 from Step: 1
    Execution finished in 29 steps
    --------------------
    Starting Iteration: 107 from Step: 1
    Execution finished in 81 steps
    --------------------
    Starting Iteration: 108 from Step: 1
    Execution finished in 17 steps
    --------------------
    Starting Iteration: 109 from Step: 1
    Execution finished in 13 steps
    --------------------
    Starting Iteration: 110 from Step: 1
    Execution finished in 13 steps
    --------------------
    Starting Iteration: 111 from Step: 1
    Execution finished in 17 steps
    --------------------
    Starting Iteration: 112 from Step: 40
    Execution finished in 70 steps
    --------------------
    Starting Iteration: 113 from Step: 1
    Execution finished in 58 steps
    --------------------
    Starting Iteration: 114 from Step: 1
    Execution finished in 174 steps
    --------------------
    Starting Iteration: 115 from Step: 1
    Execution finished in 50 steps
    --------------------
    Starting Iteration: 116 from Step: 1
    Execution finished in 37 steps
    --------------------
    Starting Iteration: 117 from Step: 1
    Execution finished in 25 steps
    --------------------
    Starting Iteration: 118 from Step: 1
    Execution finished in 17 steps
    --------------------
    Starting Iteration: 119 from Step: 1
    Execution finished in 9 steps
    --------------------
    Starting Iteration: 120 from Step: 1
    Execution finished in 21 steps
    --------------------
    Starting Iteration: 121 from Step: 1
    Execution finished in 17 steps
    --------------------
    Starting Iteration: 122 from Step: 1
    Execution finished in 58 steps
    --------------------
    Starting Iteration: 123 from Step: 1
    Execution finished in 17 steps
    --------------------
    Starting Iteration: 124 from Step: 72
    Execution finished in 178 steps
    --------------------
    Starting Iteration: 125 from Step: 1
    Execution finished in 17 steps
    --------------------
    Starting Iteration: 126 from Step: 1
    Execution finished in 21 steps
    --------------------
    Starting Iteration: 127 from Step: 1
    Execution finished in 54 steps
    --------------------
    Starting Iteration: 128 from Step: 1
    Execution finished in 9 steps
    --------------------
    Starting Iteration: 129 from Step: 1
    Execution finished in 22 steps
    --------------------
    Starting Iteration: 130 from Step: 1
    Execution finished in 58 steps
    --------------------
    Starting Iteration: 131 from Step: 1
    Execution finished in 95 steps
    --------------------
    Starting Iteration: 132 from Step: 1
    Execution finished in 14 steps
    --------------------
    Starting Iteration: 133 from Step: 1
    Execution finished in 38 steps
    --------------------
    Starting Iteration: 134 from Step: 1
    Execution finished in 13 steps
    --------------------
    Starting Iteration: 135 from Step: 1
    Execution finished in 62 steps
    --------------------
    Starting Iteration: 136 from Step: 1
    Execution finished in 82 steps
    --------------------
    Starting Iteration: 137 from Step: 19
    Execution finished in 83 steps
    --------------------
    Starting Iteration: 138 from Step: 1
    Execution finished in 63 steps
    --------------------
    Starting Iteration: 139 from Step: 1
    --------------------
    Estimated Coverage:: 1.4246418994 %
    Distinct States Explored:: 8034
    --------------------
    Explored 139 single executions
    Took 10 seconds and 0.2 GB
    Result: partially safe with 147 backtracks remaining
    --------------------
    java.lang.Exception: TIMEOUT
    at psymbolic.commandline.EntryPoint.process(EntryPoint.java:90)
    at psymbolic.commandline.EntryPoint.run(EntryPoint.java:131)
    at psymbolic.commandline.PSymbolic.main(PSymbolic.java:73)
    ```

    Here PSym explores 139 schedules, checking 8034 distinct states, and achieving an estimated coverage of ~ 1.4 %.

    Check file `output/coverage-clientserver.log` for a detailed Coverage Report.

    Check file `output/stats-clientserver.log` for a detailed Statistics Report.


### Coverage

At the end of a run, PSym reports a coverage metric as an estimated percentage of the execution tree that is explored during
the run. Assuming a uniform probability for each scheduling/data choice, this metric reports the probability of a 
randomly-sampled schedule to be bug free.

!!! danger "[Important] Does this exactly measure the % of state-space covered?"

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
For example, coverage report corresponding to running ClientServer from the [previous section](#checking-a-p-program-with-psym)
can be found in `` output/coverage-clientserver.log ``

??? example "ClientServer Coverage Report: `` output/coverage-clientserver.log ``"

    ```
    -----------------
    Coverage Report::
    -----------------
    Covered choices:    8231 scheduling,  1961 data
    Remaining choices:     0 scheduling, 30282 data
    -------------------------------------
       Step    Covered        Remaining
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
        47     65      17             190
        48     65      18             187
        49     63      15             176
        50     61      10             114
        51     61      13             148
        52     61      18             187
        53     58      14             163
        54     57      10             111
        55     57      13             148
        56     55      15             157
        57     55      10             109
        58     52      10             108
        59     50      11             127
        60     49      14             145
        61     47       9              99
        62     45       7              74
        63     44      11             116
        64     42      13             134
        65     41       5              51
        66     40      10             105
        67     40      10             113
        68     40      13             138
        69     39       5              51
        70     38       9              95
        71     38       9             103
        72     37      13             127
        73     37       5              51
        74     36       9              95
        75     36       6              65
        76     36      13             137
        77     35       5              50
        78     34       9              95
        79     33       4              45
        80     33      10             106
        81     31       3              30
        82     28       8              82
        83     26       4              42
        84     26      11             116
        85     26       3              30
        86     26       7              72
        87     26       3              31
        88     25      12             126
        89     25       3              30
        90     25       6              62
        91     25       3              31
        92     24      12             126
        93     24       2              20
        94     24       6              62
        95     23       2              21
        96     23      12             126
        97     22       2              20
        98     22       5              51
        99     22       2              20
        100     22      13             136
        101     22       2              20
        102     22       5              51
        103     22       2              20
        104     22      11             112
        105     22       3              31
        106     21       4              41
        107     21       2              20
        108     20      10             100
        109     20       2              20
        110     20       4              41
        111     19       2              20
        112     18      10             100
        113     18       2              20
        114     18       4              41
        115     18       2              20
        116     18       9              90
        117     18       2              20
        118     17       4              41
        119     17       2              20
        120     17       9              90
        121     17       2              20
        122     17       4              41
        123     17       2              20
        124     17       8              80
        125     17       2              20
        126     16       3              31
        127     16       2              20
        128     15       7              70
        129     15       2              20
        130     14       3              31
        131     14       2              20
        132     14       6              60
        133     14       2              20
        134     13       3              31
        135     13       2              20
        136     13       5              50
        137     13       2              20
        138     12       3              31
        139     12       2              20
        140     12       5              50
        141     12       2              20
        142     12       2              20
        143     12       2              20
        144     11       5              50
        145     11       2              20
        146     11       2              20
        147     11       2              20
        148     11       5              50
        149     11       2              20
        150     11       2              20
        151     11       2              20
        152     11       5              50
        153     11       2              20
        154     11       2              20
        155     11       2              20
        156     11       5              50
        157     11       2              20
        158     11       2              20
        159     11       2              20
        160     11       4              40
        161     11       2              20
        162     10       2              20
        163     10       2              20
        164     10       4              40
        165     10       2              20
        166     10       2              20
        167     10       2              20
        168     10       3              30
        169     10       2              20
        170      9       2              20
        171      9       2              20
        172      9       2              20
        173      9       2              20
        174      8       2              20
        175      8       2              20
        176      8       1              10
        177      8       1              10
        178      7       2              20
        179      6       2              20
        180      6       1              10
        181      6       1              10
        182      6       1              10
        183      6       2              20
        184      5       1              10
        185      5       1              10
        186      5       1              10
        187      5       2              20
        188      5                         
        189      5       1              10
        190      4       1              10
        191      4       1              10
        192      4                         
        193      3       1              10
        194      3       1              10
        195      3       1              10
        196      3                         
        197      3       1              10
        198      3       1              10
        199      3                         
        200      3                         
        201      2       1              10
        202      2       1              10
        203      2                         
        204      2                         
        205      2       1              10
        206      2       1              10
        207      2                         
        208      2                         
        209      2       1              10
        210      2       1              10
        211      2                         
        212      2                         
        213      2       1              10
        214      2       1              10
        215      2                         
        216      2                         
        217      2       1              10
        218      2       1              10
        219      2                         
        220      2                         
        221      2       1              10
        222      2       1              10
        223      2                         
        224      2                         
        225      2       1              10
        226      2       1              10
        227      2                         
        228      2                         
        229      2       1              10
        230      2       1              10
        231      2                         
        232      2                         
        233      2       1              10
        234      2                         
        235      2                         
        236      1                         
        237      1       1              10
        238      1                         
        239      1                         
        240      1                         
        241      1       1              10
        242      1                         
        243      1                         
        244      1                         
        245      1       1              10
        246      1                         
        247      1                         
        248      1                         
        249      1       1              10
        250      1                         
        251      1                         
        252      1                         
        253      1       1              10
        254      1                         
        255      1                         
        256      1                         
        257      1       1              10
        258      1                         
        259      1                         
        260      1                         
        261      1       1              10
        262      1                         
        263      1                         
        264      1                         
        265      1       1              10
        266      1                         
        267      1                         
        268      1                         
        269      1       1              10
        270      1                         
        271      1                         
        272      1                         
        273      1       1              10
        274      1                         
        275      1                         
        276      1                         
        277      1       1              10
        278      1                         
        279      1                         
        280      1                         
        281      1       1              10
        282      1                         
        283      1                         
        284      1                         
        285      1       1              10
        286      1                         
        287      1                         
        288      1                         
        289      1       1              10
        290      1                         
        291      1                         
        292      1                         
        293      1       1              10
        294      1                         
        295      1                         
        296      1                         
        297      1       1              10
        298      1                         
        299      1                         
        300      1                         
        301      1       1              10
        302      1                         
        303      1                         
        304      1                         
        305      1       1              10
        306      1                         
        307      1                         
        308      1                         
        309      1       1              10
        310      1                         
        311      1                         
        312      1                         
        313      1       1              10
        314      1                         
        315      1                         
        316      1                         
        317      1       1              10
        318      1                         
        319      1                         
        320      1                         
        321      1       1              10
        322      1                         
        323      1                         
        324      1                         
        325      1       1              10
        326      1                         
        327      1                         
        328      1                         
        329      1       1              10
        330      1                         
        331      1                         
        332      1                         
        333      1       1              10
        334      1                         
        335      1                         
        336      1                         
        337      1       1              10
        338      1                         
        339      1                         
        340      1                         
        341      1       1              10
        342      1                         
        343      1                         
        344      1                         
        345      1       1              10
        346      1                         
        347      1                         
        348      1                         
        349      1       1              10
        350      1                         
        351      1                         
        352      1                         
        353      1       1              10
        354      1                         
        355      1                         
        356      1                         
        357      1       1              10
        358      1                         
        359      1                         
        360      1                         
        361      1       1              10
        362      1                         
        363      1                         
        364      1                         
        365      1       1              10
        366      1                         
        367      1                         
        368      1                         
        369      1       1              10
        370      1                         
        371      1                         
        372      1                         
        373      1       1              10
        374      1                         
        375      1                         
        376      1                         
        377      1       1              10
        378      1                         
        379      1                         
        380      1                         
        381      1       1              10
        382      1                         
        383      1                         
        384      1                         
        385      1       1              10
        386      1                         
        387      1                         
        388      1                         
        389      1       1              10
        390      1                         
        391      1                         
        392      1                         
        393      1       1              10
        394      1                         
        395      1                         
        396      1                         
        397      1       1              10
        398      1                         
        399      1                         
        400      1                         
        401      1       1              10
        402      1                         
        403      1                         
        404      1                         
        405      1       1              10
        406      1                         
        407      1                         
        408      1                         
        409      1       1              10
        410      1                         
        411      1                         
        412      1                         
        413      1       1              10
        414      1                         
        415      1                         
        416      1                         
        417      1       1              10
        418      1                         
        419      1                         
        420      1                         
        421      1       1              10
        422      1                         
        423      1                         
        424      1                         
        425      1       1              10
        426      1                         
        427      1                         
        428      1                         
        429      1       1              10
        430      1                         
        431      1                         
        432      1                         
        433      1       1              10
        434      1                         
        435      1                         
        436      1                         
        437      1                         
        438      1
    ```

??? tip "Improving Coverage for ClientServer"
    Looks like this example is quite data heavy, since there are lots of unexplored data choices at different steps 
    (check the rightmost column of `` output/coverage-clientserver.log ``).
    A good idea is to reduce the amount of data non-determinism by reducing the number of choices in the 
    `choose(*)` expression, such as the number of data choices in setting the initial bank balances in expression
    `choose(100)` [here](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PTst/TestDriver.p#L30).

??? danger "[Important] Coverage report is incomplete"

    Note that the coverage report can be incomplete, since it tabulates the remaining choices as of by the end of a run.
    Each remaining choice, say at a step N, when explored by PSym can discover many more choices at steps >= N+1.


??? tip "Continuous Feedback"

    Run `.jar` with the option `` --stats 1 `` to print coverage at the end of each schedule.

    Want feedback at every step? Run with `` --stats 2 `` to print coverage after each scheduling step.


### CLI Options

Here is a list of frequently-used commandline options that can be passed to the `.jar`:

| CLI Option                | Description                                                                   |    Default    |
|---------------------------|-------------------------------------------------------------------------------|:-------------:|
| `` --method <string> ``   | Name of the test method to execute                                            |  `` auto ``   |
| `` --time-limit <sec> ``  | Time limit in seconds (use 0 for no limit)                                    |   `` 60 ``    |
| `` --memory-limit <MB> `` | Memory limit in megabytes (use 0 for no limit)                                |  `` auto ``   |
| `` --iterations <int> ``  | Number of schedules/executions to explore (use 0 for no limit)                |    `` 0 ``    |
| `` --max-steps <int> ``   | Max scheduling steps to be explored per schedule                              |  `` 1000 ``   |
| `` --seed <int> ``        | Random seed to use for the exploration                                        |    `` 0 ``    |
| `` --verbose <int> ``     | Level of verbosity in the log output                                          |    `` 0 ``    |
| `` --stats <int> ``       | Level of stats collection/reporting during the search                         |    `` 0 ``    |
| `` --mode <string> ``     | Preconfigured exploration mode to use (`` default ``, `` bmc ``, ``  fuzz ``) | `` default `` |

For a complete list of options, pass the argument ` --help `.

??? tip "Exploration Techniques"
    PSym implements a collection of configurable techniques summarized as follows:
    
    | Technique             | Description                                                                |
    |-----------------------|----------------------------------------------------------------------------|
    | Search Strategy       | Configure the order in which search is performed: `astar`, `random`, `dfs` |
    | Choice Selection      | Configure how a scheduling or data choice is selected: `random`, `none`    |
    | Never Repeat States   | Track distinct states to avoid state revisits                              |
    | Stateful Backtracking | Backtrack directly without replay                                          |
    | BMC                   | Run PSym as a bounded model checker                                        |


??? danger "[Important] Preconfigured Modes"

    For ease of usage, PSym provides a set of preconfigured exploration modes as follows:
    
    | Mode      | Description                                                                                                                                                                                                                 |
    |-----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
    | `default` | Explore single execution at a time <br/> Search Strategy = `astar` <br/> Choice Selection = `random` <br/> Never Repeat States = `ON` <br/> Stateful Backtracking = `ON` <br/> BMC = `OFF`                                  |
    | `bmc`     | Explore all executions together symbolically as a bounded model checker <br/> Search Strategy = `N/A` <br/> Choice Selection = `N/A` <br/> Never Repeat States = `OFF` <br/> Stateful Backtracking = `N/A` <br/> BMC = `ON` |
    | `fuzz`    | Explore like a random fuzzer (but never repeat an execution!) <br/> Search Strategy = `random` <br/> Choice Selection = `random` <br/> Never Repeat States = `OFF` <br/> Stateful Backtracking = `OFF` <br/> BMC = `OFF`    |
    
    Pass the CLI argument ` --mode <option> ` to set the exploration mode.


You are now a pro :drum:! Give PSym a try on your P model and shared your feedback with us.