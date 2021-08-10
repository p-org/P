We introduce the P language syntax and semantics in details in the [Tutorials](../tutsoutline.md) and [Language Manual](../manualoutline.md).
In this section, We provide an overview of the steps involved in compiling and testing a P program using the [client server](../tutorial/clientserver.md) example.

??? tip "Clone the P repo and navigate to the Tutorial folder"
    We will use the ClientServer example from Tutorial to describe the process of compiling and testing a P program. 
    Please clone the P repo and navigate to the ClientServer example in Tutorial.
    
    ```shell
    cd <P cloned folder>/Tutorial/ClientServer
    ```
### Structure of a P Program

A P program is typically divided into following four folders (or parts):

- `PSrc`: contains all the state machines representing the implementation (model) of the system or protocol to be verified or tested.
- `PSpec`: contains all the specifications representing the _correctness_ properties that the system must satisfy.
- `PTst`: contains all the _environment_ or _test harness_ state machines that model the non-deterministic 
  scenarios under which we want to check that the system model in `PSrc` satisfies the specifications in `PSpec`. 
  P allows writing different model checking scenarios as test-cases.
  
- `PForeign`: P also supports interfacing with foreign languages like `Java`, `C#`, and `C/C++`. 
  P allows programmers to implement a part of their protocol logic in these foreign languages and use them in a P program using the Foreign types and functions interface ([Foreign](../manual/foriegntypesfunctions.md))
  The `PForeign` folder contains all the foreign code used in the P program.

??? Tip "Models, Specifications, Model Checking Scenario"
    A quick primer on what a model is, versus a specification, and model checking scenarios:
    (1) A specification says what the system should do (correctness properties).
    (2) A model captures the details of how the system does it.
    (3) A model checking scenario provides the finite non-deterministc test-harness or environment under which the model checker should check
    that the system model satisfies its specifications.

!!! Note "Recommendation"
    The folder structure described above is just a recommendation. The P compiler does not require any particular folder structure for a P project.
    The examples in the [Tutorials](../tutsoutline.md) use the same folder structure.


### Compiling a P program

We assume that you have successfully installed the [P Compiler and Checker](install.md#installing-the-p-compiler).
The P compiler provides the following commandline options:

??? help "P Compiler commandline options:"
    ```shell
    ------------------------------------------
    Recommended usage:
    >> pc -proj:<.pproj file>
    ------------------------------------------
    Optional usage:
    >> pc file1.p [file2.p ...] [options]

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

There are two ways of compiling a P program:
(1) using the P project file (`*.pproj`) that provides all the inputs to the compiler.
(2) passing all the P files (`*.p`) to be compiled as commandline arguments along with other options like `-generate` to provide the code to be generated. 


!!! note "Recommendation"
    We recommend using the P project files to compile a P program.

=== "Compile using the P Project"

    The P compiler does not support fancy project management features like separate compilation and dependency analysis (coming soon).
    The current project file interface is a simple mechanism to provide all the required inputs to the compiler in XML format.
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
    In `<PFile>` can either specify the path to the P file or to a folder and the P compiler includes all the files in the folder during compilation.
    The `<Target>` block specifies the target language for code generation (options are: CSharp, C, RVM and we are adding support for Java).
    The `<ProjectName>` block provides the name for the project which is used as the output file name.
    The `<OutputDir>` block provides the output directory for the generated code.
    Finally, `<IncludeProject>` block provides path to other P projects that must be included as dependencies during compilation. 
    The P compiler simply recursively copies all the P files in the dependency projects and compiles them together. 
    This feature provides a way to split the P models for a large system into sub projects that can share models.

    Compiling the ClientServer project:
    
    ``` shell
    pc -proj:ClientServer.pproj
    ```

    ??? info "Expected output"
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
        Restored P/Tutorial/ClientServer/ClientServer.csproj (in 122 ms).
        ClientServer -> P/Tutorial/ClientServer/netcoreapp3.1/ClientServer.dll
        
        Build succeeded.
        0 Warning(s)
        0 Error(s)
        
        Time Elapsed 00:00:02.25
        ----------------------------------------
        ```

=== "Compile P files directly"
    aa

