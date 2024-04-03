!!! info "If you are using an older P version 1.x.x, please find the usage guide [here](../old/getstarted/usingP.md)"

!!! check ""
    Before moving forward, we assume that you have successfully installed
    [P](install.md) and the [Peasy extension](PeasyIDE.md) :metal:.

We introduce the P language syntax and semantics in details in the
[Tutorials](../tutsoutline.md) and [Language Manual](../manualoutline.md). In this
section, we provide an overview of the steps involved in compiling and checking a P program
using the [client server](../tutorial/clientserver.md) example in Tutorials.


??? info "Get the Client Server Example Locally"
    We will use the [ClientServer](https://github.com/p-org/P/tree/master/Tutorial/1_ClientServer) example from Tutorial folder in P repository to describe the process of compiling and checking a P program. Please clone the P repo and navigate to the
    ClientServer example in Tutorial.

    Clone P Repo locally:
    ```shell
    git clone https://github.com/p-org/P.git
    ```
    Navigate to the ClientServer examples folder:
    ```shell
    cd <P cloned folder>/Tutorial/1_ClientServer
    ```



### Compiling a P program

There are two ways of compiling a P program:

1. Using a **P project file** (`*.pproj`) to provide all the required inputs to the compiler or
2. Passing all the P files (`*.p`) along with other options **as commandline arguments** to the compiler.

!!! tip "Recommendation"
    We recommend using the P project files to compile a P program.

??? help "P Compiler commandline options"
    The P compiler provides the following commandline options:

    ```
    -----------------------------------------------
    usage: p compile [--help] [--pproj string] [--pfiles string] [--projname string] [--outdir string]

    The P compiler compiles all the P files in the project together and generates the executable that
    can be checked for correctness by the P checker.

    Compiling using `.pproj` file:
    ------------------------------
    -pp, --pproj string         : P project file to compile (*.pproj). If this option is not passed,
    the compiler searches for a *.pproj in the current folder

    Compiling P files directly through commandline:
    -----------------------------------------------
    -pf, --pfiles string        : List of P files to compile
    -pn, --projname string      : Project name for the compiled output
    -o, --outdir string         : Dump output to directory (absolute or relative path)

    Optional Arguments:
    -------------------
    -h, --help                  Show this help menu
    -----------------------------------------------
    ```

=== "Compile using the P Project"

    Compiling the ClientServer project using the P Project file:

    ``` shell
    p compile
    ```

    ??? info "Expected Output"
        ```
        $ p compile

        .. Searching for a P project file *.pproj locally in the current folder
        .. Found P project file: P/Tutorial/1_ClientServer/ClientServer.pproj
        ----------------------------------------
        ==== Loading project file: P/Tutorial/1_ClientServer/ClientServer.pproj
        ....... includes p file: P/Tutorial/1_ClientServer/PSrc/Server.p
        ....... includes p file: P/Tutorial/1_ClientServer/PSrc/Client.p
        ....... includes p file: P/Tutorial/1_ClientServer/PSrc/AbstractBankServer.p
        ....... includes p file: P/Tutorial/1_ClientServer/PSrc/ClientServerModules.p
        ....... includes p file: P/Tutorial/1_ClientServer/PSpec/BankBalanceCorrect.p
        ....... includes p file: P/Tutorial/1_ClientServer/PTst/TestDriver.p
        ....... includes p file: P/Tutorial/1_ClientServer/PTst/Testscript.p
        ----------------------------------------
        Parsing ...
        Type checking ...
        Code generation ...
        Generated ClientServer.cs.
        ----------------------------------------
        Compiling ClientServer...
        MSBuild version 17.3.1+2badb37d1 for .NET
        Determining projects to restore...
        Restored P/Tutorial/1_ClientServer/PGenerated/CSharp/ClientServer.csproj (in 102 ms).
        ClientServer -> P/Tutorial/1_ClientServer/PGenerated/CSharp/net6.0/ClientServer.dll

        Build succeeded.
        0 Warning(s)
        0 Error(s)

        Time Elapsed 00:00:02.25


        ----------------------------------------
        ~~ [PTool]: Thanks for using P! ~~
        ```

    `p compile` command searches for the `*.pproj` file in the current directory.

    If you are running `p compile` from outside the P project directory, use the `-pp <path to *.pproj>` option instead.

    ??? info "P Project File Details"
        The P compiler does not support advanced project management features like separate compilation and dependency analysis (_coming soon_).
        The current project file interface is a simple mechanism to provide all the required inputs to the compiler in an XML format ([ClientServer.pproj](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/ClientServer.pproj)).
        ``` xml
        <!-- P Project file for the Client Server example -->
        <Project>
        <ProjectName>ClientServer</ProjectName>
        <InputFiles>
            <PFile>./PSrc/</PFile>
            <PFile>./PSpec/</PFile>
            <PFile>./PTst/</PFile>
        </InputFiles>
        <OutputDir>./PGenerated/</OutputDir>
        </Project>
        ```
        The `<InputFiles>` block provides all the P files that must be compiled together for this project.
        In `<PFile>` one can either specify the path to a P file or to a folder and the P compiler includes all the `*.p` files in the folder during compilation.
        The `<ProjectName>` block provides the name for the project which is used as the output file name for the generated code.
        The `<OutputDir>` block provides the output directory for the generated code.
        Finally, the `<IncludeProject>` block provides a path to other P projects that must be included as dependencies during compilation.
        The P compiler simply recursively copies all the P files in the dependency projects (transitively including all P files in dependent projects) and compiles them together.
        This feature provides a way to split the P models for a large system into sub projects that can share models.


=== "Compile P files directly"
    Compiling the ClientServer program by passing all the required inputs as commandline arguments:

    ```shell
    p compile -pf PSpec/*.p PSrc/*.p PTst/*.p -pn ClientServer -o PGenerated
    ```

    ??? info "Expected Output"
        ```
        $ p compile -pf PSpec/*.p PSrc/*.p PTst/*.p -pn ClientServer -o PGenerated

        Parsing ...
        Type checking ...
        Code generation ...
        Generated ClientServer.cs.
        ----------------------------------------
        Compiling ClientServer...
        MSBuild version 17.3.1+2badb37d1 for .NET
        Determining projects to restore...
        Restored P/Tutorial/1_ClientServer/PGenerated/CSharp/ClientServer.csproj (in 115 ms).
        ClientServer -> P/Tutorial/1_ClientServer/PGenerated/CSharp/net6.0/ClientServer.dll

        Build succeeded.
        0 Warning(s)
        0 Error(s)

        Time Elapsed 00:00:05.74


        ----------------------------------------
        ~~ [PTool]: Thanks for using P! ~~
        ```

### Checking a P program

Compiling the ClientServer program generates a `ClientServer.dll`, this `dll` is
the C# representation of the P program. The P Checker takes as input this `dll` and
systematically explores behaviors of the program for the specified test case.

The path to the `dll` is present in the generated compilation output, check for line:
`ClientServer -> <Path>/ClientServer.dll`

You can get the list of test cases defined in the P program by running the P Checker:

```shell
p check
```

!!! info "Expected Output"
    ```hl_lines="8 9 10"
    $ p check

    .. Searching for a P compiled file locally in the current folder
    .. Found a P compiled file: P/Tutorial/1_ClientServer/PGenerated/CSharp/net6.0/ClientServer.dll
    .. Checking P/Tutorial/1_ClientServer/PGenerated/CSharp/net6.0/ClientServer.dll
    Error: We found '3' test cases. Please provide a more precise name of the test case you wish to check using (--testcase | -tc).
    Possible options are:
    tcSingleClient
    tcMultipleClients
    tcAbstractServer

    ~~ [PTool]: Thanks for using P! ~~
    ```

`p check` command searches for the `*.dll` file in the current directory.

If you are running `p check` from outside the directory where `*.dll` is compiled to, run `p check <path to *.dll>` instead.

There are three test cases defined in the ClientServer P project, and you can specify which
test case to run by using the `-tc` or `--testcase` parameter along with the `-s` parameter to
specify how many different schedules to explore when running this test case (by default the checker explores a single schedule).
*For complex systems, running for 100,000 schedules typically finds most of the easy to find bugs before
running the checker on a distributed cluster to explore billions of schedules and rule out deep bugs in the system.*

So to run the `tcSingleClient` test case for 100 schedules, we can use the following command:

```shell
p check -tc tcSingleClient -s 100
```

??? info "Expected Output"
    ```
    $ p check -tc tcSingleClient -s 100

    .. Searching for a P compiled file locally in the current folder
    .. Found a P compiled file: P/Tutorial/1_ClientServer/PGenerated/CSharp/net6.0/ClientServer.dll
    .. Checking P/Tutorial/1_ClientServer/PGenerated/CSharp/net6.0/ClientServer.dll
    .. Test case :: tcSingleClient
    ... Checker is using 'random' strategy (seed:2510398613).
    ..... Schedule #1
    ..... Schedule #2
    ..... Schedule #3
    ..... Schedule #4
    ..... Schedule #5
    ..... Schedule #6
    ..... Schedule #7
    ..... Schedule #8
    ..... Schedule #9
    ..... Schedule #10
    ..... Schedule #20
    ..... Schedule #30
    ..... Schedule #40
    ..... Schedule #50
    ..... Schedule #60
    ..... Schedule #70
    ..... Schedule #80
    ..... Schedule #90
    ..... Schedule #100
    ... Emitting coverage reports:
    ..... Writing PCheckerOutput/BugFinding/ClientServer.dgml
    ..... Writing PCheckerOutput/BugFinding/ClientServer.coverage.txt
    ..... Writing PCheckerOutput/BugFinding/ClientServer.sci
    ... Checking statistics:
    ..... Found 0 bugs.
    ... Scheduling statistics:
    ..... Explored 100 schedules
    ..... Number of scheduling points in terminating schedules: 22 (min), 139 (avg), 585 (max).
    ... Elapsed 1.4774908 sec.
    . Done
    ~~ [PTool]: Thanks for using P! ~~
    ```

There is a known bug in the ClientServer example (explained in the Tutorials) which is caught by
the `tcAbstractServer` test case. Run command:
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
    ... Checker is using 'random' strategy (seed:490949683).
    ..... Schedule #1
    Checker found a bug.
    ... Emitting traces:
    ..... Writing PCheckerOutput/BugFinding/ClientServer_0_0.txt
    ..... Writing PCheckerOutput/BugFinding/ClientServer_0_0.trace.json
    ..... Writing PCheckerOutput/BugFinding/ClientServer_0_0.dgml
    ..... Writing PCheckerOutput/BugFinding/ClientServer_0_0.schedule
    ... Elapsed 0.2264114 sec.
    ... Emitting coverage reports:
    ..... Writing PCheckerOutput/BugFinding/ClientServer.dgml
    ..... Writing PCheckerOutput/BugFinding/ClientServer.coverage.txt
    ..... Writing PCheckerOutput/BugFinding/ClientServer.sci
    ... Checking statistics:
    ..... Found 1 bug.
    ... Scheduling statistics:
    ..... Explored 1 schedule
    ..... Found 100.00% buggy schedules.
    ..... Number of scheduling points in terminating schedules: 97 (min), 97 (avg), 97 (max).
    ... Elapsed 0.4172069 sec.
    . Done
    ~~ [PTool]: Thanks for using P! ~~
    ```

The P checker on finding a bug generates two artifacts (highlighted in the expected output above):

1. A **textual trace file** (e.g., `ClientServer_0_0.txt`) that has the readable error trace representing the
sequence of steps from the initial state to the error state;
2. A **schedule file** (e.g., `ClientServer_0_0.schedule`) that can be used to replay the error
trace and single step through the P program with the generated error trace for debugging
(more details about debugging P error traces: [here](../advanced/debuggingerror.md)).





