## Debugging Error Traces

!!! info "Looking for P 1.x?"
    If you are using an older P version 1.x.x, please find the usage guide [here](../old/advanced/debuggingerror.md).

As described in the [using P compiler and checker](../getstarted/usingP.md) section, running the following command for the ClientServer example finds an error:

```shell
p check -tc tcAbstractServer -s 100
```

??? info "Expected Output"
    ```hl_lines="9 11 13 20"
    $ p check -tc tcAbstractServer -s 100

    .. Searching for a P compiled file locally in the current folder
    .. Found a P compiled file: P/Tutorial/1_ClientServer/PGenerated/CSharp/net8.0/ClientServer.dll
    .. Checking P/Tutorial/1_ClientServer/PGenerated/CSharp/net8.0/ClientServer.dll
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

---

### Bug Artifacts

The P checker on finding a bug generates two artifacts:

| Artifact | Description |
|----------|-------------|
| **Textual trace** (`*.txt`) | Readable error trace showing the sequence of steps (messages sent, received, machines created) from the initial state to the error state |
| **Schedule file** (`*.schedule`) | Can be used to replay the error trace and single-step through the P program for debugging |

---

### Reading the Error Trace

The `*.txt` file contains the sequence of steps from the initial state to the final error state. At the end of the trace is the error message, for example:

```xml
<ErrorLog> Assertion Failed: PSpec/BankBalanceCorrect.p:76:9 Bank must accept
the withdraw request for 1, bank balance is 11!
```

!!! tip ""
    In most cases, you can ignore the stack trace and information below the `ErrorLog`.

---

### Replaying the Error Schedule

You can replay the error schedule using the command line with verbose output enabled:

```shell
p check --replay <buggy>.schedule -tc <testcaseName> -v
```

For example:

```shell
p check --replay PCheckerOutput/BugFinding/ClientServer_0_0.schedule \
  -tc tcAbstractServer \
  -v
```
