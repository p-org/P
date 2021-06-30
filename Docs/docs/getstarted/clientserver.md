We introduce the P language syntax and semantics in details in the [Tutorials](../tutsoutline.md) and [Language Manual](../manualoutline.md).
In this section, We provide an overview of the steps involved in compiling and testing a P program using the [client server](../tutorial/clientserver.md) example.

??? tip "Clone the P repo and navigate to the tutorials folder"
    We will use the ClientServer example from Tutorials to describe the process of compiling and testing P program. 
    Please clone the P repo and navigate to the ClientServer example in Tutorials.
    
    ```shell
    cd <P cloned folder>/Tutorial/ClientServer
    ```
### Structure of a P Program

A P program is typically divided into following four folders (or parts):

- `PSrc`: The `PSrc` folder contains all the state machines that together represent the implementation (model) of the system or protocol to be verified or tested.
- `PSpec`: The `PSpec` folder contains all the specifications that together represent the _correctness_ properties that the implementation must satisfy.
- `PTst`: The `PTst` folder contains all the _environment_ state machines or _test harness_ state machines that model the non-deterministic 
  scenarios under which we want to check that the implementation in `PSrc` satisfies the specifications in `PSpec`. 
  P allows writing different model checking scenarios as test-cases.
  
- `PForeign`: P also supports interfacing with foreign languages like `Java`, `C#`, and `C/C++`. 
  P allows programmers to implement a part of its protocol logic in these foreign languages and use them in a P program using the Foreign types and functions interface ([Foreign](../manual/foriegntypesfunctions.md))
  The `PForeign` folder contains all the foreign code used in the P program.
  
!!! Note "Recommendation"
    The folder structure described above is a recommendation. The P compiler does not require any particular folder structure for a P project.
    The examples in the [Tutorials](../tutsoutline.md) use the same folder structure.

### Compiling a P program

We assume that you have successfully installed the [P Compiler and checker](install.md#installing-the-p-compiler).
The P compiler provides the following commandline options:

??? help "P Compiler commandline options:"
    ```shell
    ------------------------------------------
    Recommended usage:
    >> pc -proj:<.pproj file>
    ------------------------------------------
    Optional usage:
    >> pc file1.p [file2.p ...] [-t:tfile] [options]
    -t:[target project name]   -- project name (as well as the generated file)
    if not supplied, use file1
    -outputDir:[path]          -- where to write the generated files
    -aspectOutputDir:[path]    -- where to write the generated aspectj files
    if not supplied, use outputDir
    -generate:[C,CSharp,RVM]   -- select a target language to generate
    C       : generate C code
    CSharp  : generate C# code
    RVM     : generate Monitor code
    -h, -help, --help          -- display this help message
    ------------------------------------------
    ```

There are two ways of compiling a P program: 
(1) passing all the P files (`*.p`) to be compiled as commandline arguments along with the other parameters like `-generate` to provide the code to be generated or 
(2) using the P project file (`*.pproj`) that provides all the inputs to the compiler.

!!! note "Recommendation"
    We recommend using the P project files to compile a P program.

