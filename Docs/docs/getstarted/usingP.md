!!! check ""  
    Before moving forward, we assume that you have successfully installed the
    [P Compiler and Checker](install.md#step-3-install-p-compiler) and the [syntax highlighting plugin](syntaxhighlight.md) :metal: .

We introduce the P language syntax and semantics in details in the
[Tutorials](../tutsoutline.md) and [Language Manual](../manualoutline.md). In this
section, we provide an overview of the steps involved in compiling and testing a P program
using the [client server](../tutorial/clientserver.md) example.


??? info "Get the Client Server Example Locally"  
    We will use the [ClientServer](https://github.com/p-org/P/tree/master/Tutorial/ClientServer) example from Tutorial folder in P repository to describe the
    process of compiling and testing a P program. Please clone the P repo and navigate to the
    ClientServer example in Tutorial.

    Clone P Repo locally:
    ```shell 
    git clone https://github.com/p-org/P.git
    ```
    Navigate to the ClientServer examples folder:
    ```shell
    cd <P cloned folder>/Tutorial/ClientServer
    ```



### Compiling a P program

There are two ways of compiling a P program: (1) using a P project file (`*.pproj`) to
provides all the required inputs to the compiler or (2) passing the P files (`*.p`)
along with other options (e.g., `-generate`) as commandline arguments to the compiler.

!!! tip ""  
    We recommend always using the P project files to compile a P program.

??? help "P Compiler commandline options:"
    The P compiler provides the following commandline options:

    ```shell
    ------------------------------------------ 
    Recommended usage: >> pc -proj:<.pproj file>
    ------------------------------------------ 
    Optional usage: >> pc file1.p [file2.p ...][options]
    ------------------------------------------ 
    options:
    -t:[target project name]   -- project name (as well as the generated file) if not supplied, use file1
    -outputDir:[path]          -- where to write the generated files
    -aspectOutputDir:[path]    -- where to write the generated aspectj files if not supplied, use outputDir
    -generate:[C,CSharp,RVM]   -- select a target language to generate
    C       : generate C code
    CSharp  : generate C# code
    RVM     : generate Monitor code
    -h, -help, --help          -- display this help message
    ------------------------------------------
    ```

=== "Compile using the P Project"

    Compiling the ClientServer project using the P Project file:

    ``` shell
    pc -proj:ClientServer.pproj
    ```
    
    ??? info "Expected Output"
        ```
        $ pc -proj:ClientServer.pproj
        ----------------------------------------
        ==== Loading project file: ClientServer.pproj
        ....... includes p file: P/Tutorial/ClientServer/PSrc/ClientServer.p
        ....... includes p file: P/Tutorial/ClientServer/PSrc/AbstractServer.p
        ....... includes p file: P/Tutorial/ClientServer/PSpec/Monotonic.p
        ....... includes p file: /P/Tutorial/ClientServer/PTst/Modules.p
        ....... includes p file: P/Tutorial/ClientServer/PTst/TestDriver.p
        ....... includes p file: P/Tutorial/ClientServer/PTst/Testscript.p
        ----------------------------------------
        ----------------------------------------
        Parsing ..
        Type checking ...
        Code generation ....
        Generated ClientServer.cs
        ----------------------------------------
        Compiling ClientServer.csproj ..
        
        Microsoft (R) Build Engine version 16.8.3+39993bd9d for .NET
        Copyright (C) Microsoft Corporation. All rights reserved.
        
        Determining projects to restore...
        Restored P/Tutorial/ClientServer/ClientServer.csproj.
        ClientServer -> P/Tutorial/ClientServer/netcoreapp3.1/ClientServer.dll
        
        Build succeeded.
        0 Warning(s)
        0 Error(s)
        
        ----------------------------------------
        ```

    ??? info "P Project File Details"
        The P compiler does not support project management features like separate compilation and dependency analysis (coming soon).
        The current project file interface is a simple mechanism to provide all the required inputs to the compiler in a XML format.
        ``` xml
        <Project>
        <IncludeProject>../CommonUtils/Common.pproj</IncludeProject>
        <InputFiles>
            <PFile>./PSrc/</PFile>
            <PFile>./PSpec/</PFile>
            <PFile>./PTst/</PFile>
        </InputFiles>
        <Target>CSharp</Target>
        <ProjectName>ClientServer</ProjectName>
        <OutputDir>./PGenerated/</OutputDir>
        </Project>
        ```
        The `<InputFiles>` block provides all the P files that must be compiled together for this project. 
        In `<PFile>` can either specify the path to a P file or to a folder and the P compiler includes all the `*.p` files in the folder during compilation.
        The `<Target>` block specifies the target language for code generation (options are: CSharp, C, RVM and we are adding support for Java).
        The `<ProjectName>` block provides the name for the project which is used as the output file name for the generated code.
        The `<OutputDir>` block provides the output directory for the generated code.
        Finally, `<IncludeProject>` block provides path to other P projects that must be included as dependencies during compilation. 
        The P compiler simply recursively copies all the P files in the dependency projects (transitively including all P files in dependent projects) and compiles them together. 
        This feature provides a way to split the P models for a large system into sub projects that can share models.

=== "Compile P files directly"
    Compiling the ClientServer program by passing all the required inputs as commandline arguments:

    ```shell
    pc PSpec/*.p PSrc/*.p PTst/*.p \ 
    -generate:csharp -outputDir:PGenerated -target:ClientServer
    ```
    
    ??? info "Expected Output"
    ```----------------------------------------
    ....... includes p file: P/Tutorial/ClientServer/PSpec/Monotonic.p
    ....... includes p file: P/Tutorial/ClientServer/PSrc/AbstractServer.p
    ....... includes p file: P/Tutorial/ClientServer/PSrc/ClientServer.p
    ....... includes p file: P/Tutorial/ClientServer/PTst/Modules.p
    ....... includes p file: P/Tutorial/ClientServer/PTst/TestDriver.p
    ....... includes p file: P/Tutorial/ClientServer/PTst/Testscript.p
    ----------------------------------------
    ----------------------------------------
    Parsing ..
    Type checking ...
    Code generation ....
    Generated ClientServer.cs
    ----------------------------------------
    Compiling ClientServer.csproj ..
    
    Microsoft (R) Build Engine version 16.10.2+857e5a733 for .NET
    Copyright (C) Microsoft Corporation. All rights reserved.
    
      Determining projects to restore...
      All projects are up-to-date for restore.
      ClientServer -> /Users/ankushpd/Workspace/github/P/Tutorial/ClientServer/PGenerated/netcoreapp3.1/ClientServer.dll
    
    Build succeeded.
        0 Warning(s)
        0 Error(s)
    ```

### Testing a P program

Compiling the ClientServer program generates a `ClientServer.dll`, this `dll` is
the C# representation of the P program. The P Checker takes as input this `dll` and
systematically explores behaviors of the program for the specific test case.

The path to the `dll` is present in the generated compilation output, check for line:
`ClientServer -> <Path>/ClientServer.dll`

You can get the list of test cases defined in the P program by passing the generated `dll`
to the P Checker:

```shell
pmc <Path>/ClientServer.dll
```

Expected Output:

```shell hl_lines="4 5 6"
pmc <Path>/ClientServer.dll

Provide /method flag to qualify the test method name you wish to use. 
Possible options are::
PImplementation.singleClientServer.Execute
PImplementation.multipleClientsServer.Execute
PImplementation.singleClientServerWithLiveness.Execute
```
There are three test cases defined in the ClientServer P project and you can specify which
test case to run by using the `-m` or `/method` parameter along with the `-i` parameter to
specify how many different schedules to explore when running this test case (by default the checker explores a single schedule).
For complex systems, running for 100,000 schedules typically finds most of the easy to find bugs before
running the checker on a distributed cluster to explore billions of schedules and rule out deep bugs in the system.

So you test the `singleClientServer` test case for 100 schedules, we can use the following command:

```
pmc <Path>/ClientServer.dll \
    -m PImplementation.singleClientServer.Execute \
    -i 100
```

??? info "Expected Output"

    ```
    pmc <Path>/ClientServer.dll -m PImplementation.singleClientServer.Execute -i 100
    . Testing <Path>/ClientServer.dll
    ... Method PImplementation.singleClientServer.Execute
    Starting TestingProcessScheduler in process 61218
    ... Created '1' testing task.
    ... Task 0 is using 'random' strategy (seed:3216586065).
    ..... Iteration #1
    ..... Iteration #2
    ..... Iteration #3
    ..... Iteration #4
    ..... Iteration #5
    ..... Iteration #6
    ..... Iteration #7
    ..... Iteration #8
    ..... Iteration #9
    ..... Iteration #10
    ..... Iteration #20
    ..... Iteration #30
    ..... Iteration #40
    ..... Iteration #50
    ..... Iteration #60
    ..... Iteration #70
    ..... Iteration #80
    ..... Iteration #90
    ..... Iteration #100
    ... Testing statistics:
    ..... Found 0 bugs.
    ... Scheduling statistics:
    ..... Explored 100 schedules: 100 fair and 0 unfair.
    ..... Number of scheduling points in fair terminating schedules: 30 (min), 30 (avg), 30 (max).
    ... Elapsed 0.407219 sec.
    . Done
    ```

There is a known bug in the ClientServer example (explain in the Tutorials) which is caught by
the `multipleClientsServer` test case. Run command:
```
pmc <Path>/ClientServer.dll \
    -m PImplementation.multipleClientsServer.Execute \
    -i 100
```

??? info "Expected Output"
    ``` hl_lines="9 11 12 15"
    pmc <Path>/ClientServer.dll -m PImplementation.multipleClientsServer.Execute -i 100

    . Testing <Path>/ClientServer.dll
    ... Method PImplementation.multipleClientsServer.Execute
    Starting TestingProcessScheduler in process 62234
    ... Created '1' testing task.
    ... Task 0 is using 'random' strategy (seed:719852850).
    ..... Iteration #1
    ... Task 0 found a bug.
    ... Emitting task 0 traces:
    ..... Writing <Path>/ClientServer_0_0.txt
    ..... Writing <Path>/ClientServer_0_0.schedule
    ... Elapsed 0.2006624 sec.
    ... Testing statistics:
    ..... Found 1 bug.
    ... Scheduling statistics:
    ..... Explored 1 schedule: 1 fair and 0 unfair.
    ..... Found 100.00% buggy schedules.
    ..... Number of scheduling points in fair terminating schedules: 17 (min), 17 (avg), 17 (max).
    ... Elapsed 0.3386085 sec.
    . Done
    ```

The P checker on finding a bug generates two artifacts (highlighted in the expected output above):
(1) a text file (e.g., `ClientServer_0_0.txt`) that has the readable error trace representing the
sequence of steps from the intial state to the error state.
(2) a schedule file (e.g., `ClientServer_0_0.schedule`) that can be used to replay the error
trace and single step through the P program for the generated error trace for debugging
(more details about debugging P error traces: [here](../howtoguides/debuggingerror.md)).





