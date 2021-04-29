This repo presents three examples of increasing complexity used to
illustrate various features of the P programming language and framework.

- **Client Server**: A simple client-server example where a client state
  machine sends requests to a server state machine that performs some
  local computation and sends a response back to the client.
  - We will use this example to understand P syntax, semantics, how to
    write specifications, run the model checker to find a bug, and then
    fix the bug.
- **Two Phase Commit**: Two phase commit protocol where a coordinator
  state machine communicates with participant state machines to ensure
  atomicity guarantees for transactions.
  - We will use this example to dive deeper into how to model
    non-determinism in systems, how to write more complex properties in
    P, run the model checker, find and fix the existing bug.
- **Failure Detector**: We will use this protocol as an exercise to
  understand how to model node failures in P.

## Getting Started

### [Step 1] Installing the P framework

* There are two prerequisits for P:
  - [Microsoft .NET Core SDK (3.1)](https://dotnet.microsoft.com/download/dotnet-core/3.1)

    [Recommended for MacOS] Install using brew
    [link](https://formulae.brew.sh/cask/dotnet-sdk): `brew install
    --cask dotnet-sdk`

    Successful installation can be verified with `dotnet --info`.

  - Java JRE.

    On Ubuntu this can be installed via apt: `sudo apt-get install
    default-jre`

    On MacOS an installer can be downloaded from the
    [official Java downloads page](https://java.com/en/download/manual.jsp)

    Successful installation can be verified with `java -version`.

* Run the following command to install the P compiler and P model
  checker (coyote). You can find more information on the
  [P wiki](https://github.com/p-org/P/wiki).

  ```console
  > dotnet tool install --global P
  > dotnet tool install --global Microsoft.Coyote.CLI --version 1.0.5
  ```

* Recommended: Add an alias `pmc`

  ```console
    > alias pmc='coyote test'
  ```

### [Step 2] Compiling the Client Server P Project

A `*.pproj` P project file provides all the details required for the P
compiler to compile your project. The command below will compile the
ClientServer P project and generate the `ClientServer.cs` file in the
`PGenerated` folder. It will then compile the generated code to build a
`dll` which is a C# representation of the P program.

```console
> cd ClientServer
> pc -proj:ClientServer.pproj
```

### [Step 3] Running the P Checker

We now run the P model-checker (coyote) on the Client Server example.
`pmc` provides a lot of options but for now we will only use a fix set
of options.

```console
> pmc <path to the dll generated above> -i 1000 -m <test case name you want to run>
```

For example:

```console
> pmc ClientServer/netcoreapp3.1/ClientServer.dll -i 1000 -m ClientServer.singleClientServer.Execute
```

