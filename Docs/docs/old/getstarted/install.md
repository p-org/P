# Installing P

P is built to be cross-platform and can be used on MacOS, Linux, and Windows. We provide a step-by-step guide for installing P along with its required dependencies.

!!! info ""
After each step, please use the troubleshooting check to ensure that each installation step succeeded.

### [Step 1] Install .Net Core SDK
The P compiler and checker are implemented in C# and hence the tool chain requires `dotnet`.
P currently uses the specific version of [.Net SDK 3.1](https://dotnet.microsoft.com/download/dotnet/3.1).
To install .Net Core 3.1 SDK use:

=== "MacOS"

    Installing .Net SDK on MacOS using Homebrew ([details](https://formulae.brew.sh/cask/dotnet))
    ```
    brew tap isen-ng/dotnet-sdk-versions
    brew install --cask dotnet-sdk3-1-400
    ```
    Dont have Homebrew? :upside_down_face: Install directly using the [installer](https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-3.1.412-macos-x64-installer).

=== "Ubuntu"

    Installing .Net SDK on Ubuntu ([details](https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu))

    ```
    wget https://packages.microsoft.com/config/ubuntu/21.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    ```

    ```
    sudo apt-get update; \
    sudo apt-get install -y apt-transport-https && \
    sudo apt-get update && \
    sudo apt-get install -y dotnet-sdk-3.1
    ```

=== "Amazon Linux"
Installing .Net SDK on Amazon Linux ([details](https://docs.servicestack.net/deploy-netcore-to-amazon-linux-2-ami))

    ```
    sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
    ```

    ```
    sudo yum install dotnet-sdk-3.1
    ```

=== "Windows"

    Installing .Net SDK on Windows using the installer ([details](https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-3.1.412-windows-x64-installer))

??? hint "Troubleshoot: Confirm that dotnet is correctly installed on your machine."
`dotnet --list-sdks`

    You must see an SDK with `3.1.*` dotnet version installed.
    If you get `dotnet` command not found error, mostly likely, you need to add the path to dotnet in your `PATH`.

### [Step 2] Install Java Runtime

P compiler uses [ANTLR](https://www.antlr.org/) parser and hence requires `java`.
If you already have Java installed :innocent:, ignore this step.
To install Java use:

=== "MacOS"

    Installing Java on MacOS using Homebrew ([details](https://mkyong.com/java/how-to-install-java-on-mac-osx/))
    ```
    brew install java
    ```
    Dont have Homebrew? Directly use [installer](https://www.java.com/en/download/help/mac_install.html).

=== "Ubuntu"

    Installing Java on Ubuntu ([details](https://ubuntu.com/tutorials/install-jre#2-installing-openjdk-jre))

    ```
    sudo apt install default-jre
    ```

=== "Amazon Linux"

    Installing Java 11 on Amazon Linux (you can use any version of java >= 9)

    ```
    sudo yum install java-11-amazon-corretto-devel
    ```

=== "Windows"

    Installing Java on Windows ([details](https://www.java.com/en/download/help/windows_manual_download.html))

??? hint "Troubleshoot: Confirm that java is correctly installed on your machine."
`java -version`

    If you get `java` command not found error, mostly likely, you need to add the path to `java` in your `PATH`.

### [Step 3] Install P Compiler

Install the P compiler as a `dotnet tool` using the following command:

```
dotnet tool install --global P
```

??? hint "Troubleshoot: Confirm that `pc` is correctly installed on your machine"

    After installation, run `which pc` and it should show:
    ```shell
    which pc
    /Users/<user>/.dotnet/tools/pc
    ```
    If not, add `$HOME/.dotnet/tools` to `$PATH` in your `.bash_profile` (or equivalent) and try again after restarting the shell.
    If you are getting the error that the `pc` command is not found, it is most likely that `$HOME/.dotnet/tools` is not in your `PATH`.

??? help "Updating P Compiler"
You can update the version of `P` compiler by running the following command:

    `dotnet tool update --global P`

### [Step 4] Install P Checker

The current P checker depends on [Coyote](https://microsoft.github.io/coyote/) (previously [P#](https://github.com/p-org/PSharp))

Install the `Coyote` version `1.0.5` using the following command:

```
dotnet tool install --global Microsoft.Coyote.CLI --version 1.0.5
```

??? hint "Troubleshoot: Confirm that `coyote` is correctly installed on your machine"

    After installation, run `which coyote` and it should show:
    ```shell
    which coyote
    coyote is /Users/<user>/.dotnet/tools/coyote
    ```
    If not, add `$HOME/.dotnet/tools` to `$PATH` in your `.bash_profile` (or equivalent) and try again after restarting the shell.
    If you are getting an error that `coyote` command not found, its most likely that `$HOME/.dotnet/tools` is not in your `PATH`.

We highly recommend that you create the following alias as we use it in the rest of tutorials and getting started guide:

=== "On MacOS or Linux"

    Add following alias to the bash profile (`~/.bash_profile` or the equivalent on your system)
    so that you can invoke the P checker (`pmc`) directly.
    ```shell
    alias pmc='coyote test'
    ```

=== "On Windows"

    We recommend that you add the following to the `Microsoft.PowerShell_profile`
    normally found in `D:\Users\<username>\Documents\WindowsPowerShell`

    ```shell
    function pmc { coyote test $args }
    ```

- For debugging generated C# code, we recommend using [Rider](https://www.jetbrains.com/rider/) for Mac/Linux or [Visual Studio 2019](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio) for Windows.

- For debugging generated Java code, we recommend using [IntelliJ IDEA](https://www.jetbrains.com/idea/)

## Using P

Great :smile:! You are all set to compile and test your first P program :mortar_board:!