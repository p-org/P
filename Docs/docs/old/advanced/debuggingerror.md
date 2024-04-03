As described in the [using P compiler and checker](../getstarted/usingP.md) section, running the following command for the ClientServer example finds an error.

```
pmc <Path>/ClientServer.dll \
    -m PImplementation.tcSingleClientAbstractServer.Execute \
    -i 100
```

??? info "Expected Output"
    ``` hl_lines="9 11 12 15"
    pmc <Path>/ClientServer.dll -m PImplementation.tcSingleClientAbstractServer.Execute -i 100

    . Testing <Path>/ClientServer.dll
    ... Method PImplementation.tcSingleClientAbstractServer.Execute
    Starting TestingProcessScheduler in process 72578
    ... Created '1' testing task.
    ... Task 0 is using 'random' strategy (seed:574049731).
    ..... Iteration #1
    ... Task 0 found a bug.
    ... Emitting task 0 traces:
    ..... Writing /POutput/netcoreapp3.1/Output/ClientServer.dll/CoyoteOutput/ClientServer_0_0.txt
    ..... Writing /POutput/netcoreapp3.1/Output/ClientServer.dll/CoyoteOutput/ClientServer_0_0.schedule
    ... Elapsed 0.1971223 sec.
    ... Testing statistics:
    ..... Found 1 bug.
    ... Scheduling statistics:
    ..... Explored 1 schedule: 1 fair and 0 unfair.
    ..... Found 100.00% buggy schedules.
    ..... Number of scheduling points in fair terminating schedules: 132 (min), 132 (avg), 132 (max).
    ... Elapsed 0.3081316 sec.
    . Done

    ```

The P checker on finding a bug generates two artifacts (highlighted in the expected output above):
(1) a **textual trace file** (e.g., `ClientServer_0_0.txt`) that has the readable error trace representing the
sequence of steps from the intial state to the error state.
(2) a **schedule file** (e.g., `ClientServer_0_0.schedule`) that can be used to replay the error
trace and single step through the P program with the generated error trace for debugging.

### Error Trace

The `*.txt` file contains a textual error trace representing the sequence of steps (i.e, messages sent, messages received, machines created) from the initial state to the final error state. In the end of the error trace is the final error message, for example, in the case of ClientServer example above, you must see the following in the end of the error trace.

``` xml
<ErrorLog> Assertion Failed: Bank must accept the with draw request for 1,
  bank balance is 11!
```

In most cases, you can ignore the stack trace and information below the `ErrorLog`.

### Error Schedule (single stepping through error trace)

In certain cases, the trace log is not enough for debugging the issue and we would like to single step through the error state and look at the internal state of the state machines that is not captured in the trace log. We present the step-by-step guide to show how we can use the `.schedule` file to single step through the C# representation of the P program.

#### [Step 0]: Install Rider (or Visual Studio)

!!! info ""
    Before starting, we recommend that you have [Rider](https://www.jetbrains.com/rider/) installed on your machine, we will use Rider's C# debugger.

#### [Step 1]: Update the `Test.cs` file

On compiling any P project, the compiler generates two files a `Test.cs` file and a C# project `*.csproj` file.
For debugging the error trace, you must open the generated `csproj` file (its generally, `<ProjectName>.csproj`) using Rider and then edit the `Test.cs` file as follows:

```c# hl_lines="7 9" linenums="1"
public class _TestRegression {
  public static void Main(string[] args)
  {
    Configuration configuration = Configuration.Create();
    configuration.WithVerbosityEnabled(true);
    // update the path to the schedule file.
    string schedule = File.ReadAllText("absolute path to *.schedule file");
    configuration.WithReplayStrategy(schedule);
    TestingEngine engine = TestingEngine.Create(configuration, (Action<IActorRuntime>)PImplementation.<Name of the test case>.Execute);
    engine.Run();
    string bug = engine.TestReport.BugReports.FirstOrDefault();
        if (bug != null)
    {
        Console.WriteLine(bug);
    }

  }
}
```

- **Update the path of .schedule file**: Edit the `Test.cs` file and update the line 7 above with the absolute path to the `.schedule` file that represents the error schedule you want to debug.

- **Update the test case name**: Update the test case name on line 9 above. In our case, for the clientserver example it would become `PImplementation.tcSingleClientAbstractServer.Execute`.

#### [Step 2]: Add breakpoints to start debugging

You must next add the necessary break points in the Generated C# code. If you want to stop the execution at a particular P function/handler, locate that function in the generated code and add a break point. Now, you can start debugging using Rider just like you would debug anyother C# or Java program.

For example, you can add a breakpoint at line 904 in the generated code ClientServer.cs in PGenerated folder to hit the assertion that failed.
```
String.Format("Bank must accept the with draw request for {0}, bank balance is {1}!",TMP_tmp38,TMP_tmp40);
```

### Replaying the Error Schedule

One can also replay the error schedule using commandline and enabling verbose feature to dump out the error trace on the commandline.

```
pmc <path to dll>.dll --schedule <buggy>.schedule -m <testcaseName> -v
```

For example,

```
pmc ClientServer.dll --schedule ClientServer_0_0.schedule -m PImplementation.tcSingleClientAbstractServer.Execute -v
```
