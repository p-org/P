If you plan to contribute a Pull Request to P then you need to be able to build the source code
and run the tests.

### Prerequisites
- [.NET 3.1 SDK](https://dotnet.microsoft.com/download/dotnet/3.1)

- [Java JRE](http://www.oracle.com/technetwork/java/javase/downloads/index.html) [^1]

[^1]: P compiler uses [ANTLR](https://www.antlr.org/) parser generator that using Java.

**Optional:**

- For developing P programs: [IntelliJ IDEA](https://www.jetbrains.com/idea/), we support basic [P syntax highlighting](syntaxhighlight.md) for IntelliJ.

- For editing C# code (P compiler): [Rider](https://www.jetbrains.com/rider/) or [Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/) or [Visual Studio 2019](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio).

- For editing Java code (P Java runtime): [IntelliJ IDEA](https://www.jetbrains.com/idea/)

### Building the P project

Clone the [P repo](https://github.com/p-org/P) and run the following `build` script.

=== "on MacOS or Linux"
    ```shell
    cd Bld
    ./build.sh
    ```

=== "On Windows"

    ```shell
    cd Bld
    ./build.ps1
    ```

### Running the tests
You can run the following command to build and run the test regressions for P Compiler. Make sure you are in the root directory of the clone repo that has the `P.sln`.

```plain
dotnet build --configuration Release
dotnet test --configuration Release
```