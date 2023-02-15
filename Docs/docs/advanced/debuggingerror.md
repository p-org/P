??? info "Check out the guide for P version 1.x.x [here](../old/advanced/debuggingerror.md)"

As described in the [using P compiler and checker](../getstarted/usingP.md) section, running the following command for the ClientServer example finds an error.

```shell
p check -tc tcSingleClientAbstractServer -i 100
```

??? info "Expected Output"
    ```hl_lines="9 11 13 20"
    $ p check <Path>/ClientServer.dll -tc tcSingleClientAbstractServer -i 100

    .. Searching for a P compiled file locally in the current folder
    .. Found a P compiled file: P/Tutorial/1_ClientServer/PGenerated/POutput/net6.0/ClientServer.dll
    Testing P/Tutorial/1_ClientServer/PGenerated/POutput/net6.0/ClientServer.dll
    Test case :: tcSingleClientAbstractServer
    ... Checker is using 'random' strategy (seed:1624438318).
    ..... Iteration #1
    Checker found a bug.
    ... Emitting traces:
    ..... Writing P/Tutorial/1_ClientServer/PGenerated/POutput/net6.0/Output/ClientServer.dll/POutput/ClientServer_0_0.txt
    ..... Writing P/Tutorial/1_ClientServer/PGenerated/POutput/net6.0/Output/ClientServer.dll/POutput/ClientServer_0_0.dgml
    ..... Writing P/Tutorial/1_ClientServer/PGenerated/POutput/net6.0/Output/ClientServer.dll/POutput/ClientServer_0_0.schedule
    ... Elapsed 0.1909048 sec.
    ... Emitting coverage reports:
    ..... Writing P/Tutorial/1_ClientServer/PGenerated/POutput/net6.0/Output/ClientServer.dll/POutput/ClientServer.dgml
    ..... Writing P/Tutorial/1_ClientServer/PGenerated/POutput/net6.0/Output/ClientServer.dll/POutput/ClientServer.coverage.txt
    ..... Writing P/Tutorial/1_ClientServer/PGenerated/POutput/net6.0/Output/ClientServer.dll/POutput/ClientServer.sci
    ... Testing statistics:
    ..... Found 1 bug.
    ... Scheduling statistics:
    ..... Explored 1 schedule: 1 fair and 0 unfair.
    ..... Found 100.00% buggy schedules.
    ..... Number of scheduling points in fair terminating schedules: 77 (min), 77 (avg), 77 (max).
    ... Elapsed 0.3328236 sec.
    . Done
    ```

The P checker on finding a bug generates two artifacts (highlighted in the expected output above):

1. A **textual trace file** (e.g., `ClientServer_0_0.txt`) that has the readable error trace representing the
sequence of steps from the intial state to the error state.
2. A **schedule file** (e.g., `ClientServer_0_0.schedule`) that can be used to replay the error
trace and single step through the P program with the generated error trace for debugging.

### Error Trace

The `*.txt` file contains a textual error trace representing the sequence of steps (i.e, messages sent, messages received, machines created) from the initial state to the final error state. In the end of the error trace is the final error message, for example, in the case of ClientServer example above, you must see the following in the end of the error trace.

``` xml
<ErrorLog> Assertion Failed: Bank must accept the with draw request for 2, bank balance is 12!
```

In most cases, you can ignore the stack trace and information below the `ErrorLog`.

### Replaying the Error Schedule

One can also replay the error schedule using commandline and enabling verbose feature to dump out the error trace on the commandline.

```shell
p check --replay <buggy>.schedule -tc <testcaseName> -v
```

For example,

```shell
p check --replay PGenerated/POutput/net6.0/Output/ClientServer.dll/POutput/ClientServer_0_0.schedule \
 -tc tcSingleClientAbstractServer \
 -v
```
