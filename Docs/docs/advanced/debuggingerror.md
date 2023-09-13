!!! info "If you are using an older P version 1.x.x, please find the usage guide [here](../old/advanced/debuggingerror.md)"

As described in the [using P compiler and checker](../getstarted/usingP.md) section, running the following command for the ClientServer example finds an error.

```shell
p check -tc tcAbstractServer -s 100
```

??? info "Expected Output"
    ```hl_lines="9 11 13 20"
    $ p check -tc tcAbstractServer -s 100

    .. Searching for a P compiled file locally in the current folder
    .. Found a P compiled file: P/Tutorial/1_ClientServer/PGenerated/CSharp/net6.0/ClientServer.dll
    .. Checking P/Tutorial/1_ClientServer/PGenerated/CSharp/net6.0/ClientServer.dll
    .. Test case :: tcAbstractServer
    ... Checker is using 'random' strategy (seed:3584517644).
    ..... Schedule #1
    Checker found a bug.
    ... Emitting traces:
    ..... Writing PCheckerOutput/BugFinding/ClientServer_0_0.txt
    ..... Writing PCheckerOutput/BugFinding/ClientServer_0_0.trace.json
    ..... Writing PCheckerOutput/BugFinding/ClientServer_0_0.dgml
    ..... Writing PCheckerOutput/BugFinding/ClientServer_0_0.schedule
    ... Elapsed 0.2196562 sec.
    ... Emitting coverage reports:
    ..... Writing PCheckerOutput/BugFinding/ClientServer.dgml
    ..... Writing PCheckerOutput/BugFinding/ClientServer.coverage.txt
    ..... Writing PCheckerOutput/BugFinding/ClientServer.sci
    ... Checking statistics:
    ..... Found 1 bug.
    ... Scheduling statistics:
    ..... Explored 1 schedule
    ..... Found 100.00% buggy schedules.
    ..... Number of scheduling points in terminating schedules: 87 (min), 87 (avg), 87 (max).
    ... Elapsed 0.4099731 sec.
    . Done
    ~~ [PTool]: Thanks for using P! ~~
    ```

The P checker on finding a bug generates two artifacts (highlighted in the expected output above):

1. A **textual trace file** (e.g., `ClientServer_0_0.txt`) that has the readable error trace representing the
sequence of steps from the intial state to the error state.
2. A **schedule file** (e.g., `ClientServer_0_0.schedule`) that can be used to replay the error
trace and single step through the P program with the generated error trace for debugging.

### Error Trace

The `*.txt` file contains a textual error trace representing the sequence of steps (i.e, messages sent, messages received, machines created) from the initial state to the final error state. In the end of the error trace is the final error message, for example, in the case of ClientServer example above, you must see the following in the end of the error trace.

``` xml
<ErrorLog> Assertion Failed: PSpec/BankBalanceCorrect.p:76:9 Bank must accept the withdraw request for 1, bank balance is 11!
```

In most cases, you can ignore the stack trace and information below the `ErrorLog`.

### Replaying the Error Schedule

One can also replay the error schedule using commandline and enabling verbose feature to dump out the error trace on the commandline.

```shell
p check --replay <buggy>.schedule -tc <testcaseName> -v
```

For example,

```shell
p check --replay PCheckerOutput/BugFinding/ClientServer_0_0.schedule \
 -tc tcAbstractServer \
 -v
```
