## Building from Source

If you plan to contribute a Pull Request to P then you need to build the source code
and run the tests.

!!! note "Prerequisites"
    Make sure you have followed the steps in the [installation guide](install.md) to install P dependencies.

---

### Building the P Project

Clone the [P repo](https://github.com/p-org/P) and run the build script:

=== "MacOS / Linux"

    ```shell
    cd Bld
    ./build.sh
    ```

=== "Windows"

    ```shell
    cd Bld
    ./build.ps1
    ```

---

### Running the Tests

Build and run the test regressions for the P Compiler. Make sure you are in the root directory of the cloned repo that has the `P.sln`:

```shell
dotnet build --configuration Release
dotnet test --configuration Release
```

---

### Using a Local Build

P is distributed as a [dotnet tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools). To test changes locally:

1. Uninstall the existing global tool:
    ```shell
    dotnet tool uninstall --global P
    ```
2. Navigate to the command-line project:
    ```shell
    cd Src/PCompiler/PCommandLine
    ```
3. Pack a local build:
    ```shell
    dotnet pack PCommandLine.csproj --configuration Release --output ./publish \
        -p:PackAsTool=true \
        -p:ToolCommandName=P \
        -p:Version=<pick a version>
    ```
4. Install from the local package:
    ```shell
    dotnet tool install P --global --add-source ./publish
    ```
