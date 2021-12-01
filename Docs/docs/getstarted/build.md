If you plan to contribute a Pull Request to P then you need to build the source code
and run the tests. Please make sure that you have followed the steps in the [installation guide](install.md) to install P dependencies.

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

```shell
dotnet build --configuration Release
dotnet test --configuration Release
```
