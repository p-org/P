??? info "Check out the guide for P version 1.x.x [here](../old/getstarted/install.md)"

P is built to be cross-platform and can be used on MacOS, Linux, and Windows. We provide a step-by-step guide for installing P along with its required dependencies.

!!! info ""
    After each step, please use the troubleshooting check to ensure that each installation step succeeded.

### [Step 1] Install .Net Core SDK
The P compiler and checker are implemented in C# and hence the tool chain requires `dotnet`.
P currently uses the specific version of [.Net SDK 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).
To install .Net Core 6.0 SDK use:

=== "MacOS"

    Installing .Net SDK on MacOS using Homebrew ([details](https://formulae.brew.sh/cask/dotnet))

    ```shell
    brew tap isen-ng/dotnet-sdk-versions
    brew install --cask dotnet-sdk6-0-400
    ```

    Dont have Homebrew? :upside_down_face: Install directly using the installer for [x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.405-macos-x64-installer) or [Arm64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.405-macos-arm64-installer).

=== "Ubuntu"

    Installing .Net SDK on Ubuntu ([details](https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu))
    
    ```shell
    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    ```

    ```shell
    sudo apt update && sudo apt install -y dotnet-sdk-6.0
    ```


=== "Amazon Linux"
    
    Installing .Net SDK on Amazon Linux ([details](https://docs.servicestack.net/deploy-netcore-to-amazon-linux-2-ami))

    ```shell
    sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
    ```

    ```shell
    sudo yum install -y dotnet-sdk-6.0
    ```

=== "Windows"

    Installing .Net SDK on Windows using the installer for [x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.405-windows-x64-installer) or [Arm64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.405-windows-arm64-installer)

??? hint "Troubleshoot: Confirm that dotnet is correctly installed on your machine."
    ```shell
    dotnet --list-sdks
    ```

    You must see an SDK with `6.0.*` dotnet version installed.
    If you get `dotnet` command not found error, mostly likely, you need to add the path to dotnet in your `PATH`.
    
    Useful resources:

    - Ubuntu: [../fxr\] does not exist](https://stackoverflow.com/questions/73753672/a-fatal-error-occurred-the-folder-usr-share-dotnet-host-fxr-does-not-exist) 




### [Step 2] Install Java Runtime

The P compiler uses [ANTLR](https://www.antlr.org/) parser and hence requires `java`.
If you already have Java installed :innocent:, ignore this step.
To install Java use:

=== "MacOS"

    Installing Java on MacOS using Homebrew ([details](https://mkyong.com/java/how-to-install-java-on-mac-osx/))
    ```shell
    brew install java
    ```
    Dont have Homebrew? Directly use [installer](https://www.java.com/en/download/help/mac_install.html). 

=== "Ubuntu"

    Installing Java on Ubuntu ([details](https://ubuntu.com/tutorials/install-jre#2-installing-openjdk-jre))
    
    ```shell
    sudo apt install -y default-jre
    ```

=== "Amazon Linux"

    Installing Java 11 on Amazon Linux (you can use any version of java >= 9)

    ```shell
    sudo yum install -y java-11-amazon-corretto-devel
    ```

=== "Windows"

    Installing Java on Windows ([details](https://www.java.com/en/download/help/windows_manual_download.html))

??? hint "Troubleshoot: Confirm that java is correctly installed on your machine."
    ```shell
    java -version
    ```

    If you get `java` command not found error, mostly likely, you need to add the path to `java` in your `PATH`.


### [Step 3] Install P Compiler

Install the P compiler as a `dotnet tool` using the following command:

```shell
dotnet tool install --global P
```

??? hint "Troubleshoot: Confirm that `p` is correctly installed on your machine"

    After installation, run `which p` and it should show:
    ```shell
    which p
    /Users/<user>/.dotnet/tools/p
    ```
    If not, add `$HOME/.dotnet/tools` to `$PATH` in your `.bash_profile` (or equivalent) and try again after restarting the shell.
    If you are getting the error that the `p` command is not found, it is most likely that `$HOME/.dotnet/tools` is not in your `PATH`.

??? help "Updating P Compiler"
    You can update the version of `P` compiler by running the following command:

    ```shell
    dotnet tool update --global P
    ```

### [Step 4] Recommended IDE (Optional)

- For developing P programs, we recommend using [IntelliJ
  IDEA](https://www.jetbrains.com/idea/) as we support basic [P syntax
  highlighting](syntaxhighlight.md) for IntelliJ.  There is also a [plugin for
  the Vim editor](https://github.com/dijkstracula/vim-plang), which IntelliJ
  will automatically use when [Vim
  emulation](https://plugins.jetbrains.com/plugin/164-ideavim) is enabled.

- For debugging generated C# code, we recommend using [Rider](https://www.jetbrains.com/rider/) for Mac/Linux or [Visual Studio 2019](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio) for Windows.

- For debugging generated Java code, we recommend using [IntelliJ IDEA](https://www.jetbrains.com/idea/)

## Using P

 Great :smile:! You are all set to compile and test your first P program :mortar_board:!
