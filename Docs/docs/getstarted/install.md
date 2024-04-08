!!! info "If you want to use older P version 1.x.x, please use the installation steps [here](../old/getstarted/install.md)"

P is built to be cross-platform and can be used on MacOS, Linux, and Windows. We provide a step-by-step guide for installing P along with the required dependencies.

!!! success ""
    After each step, please use the troubleshooting check to ensure that each installation step succeeded.

### [Step 1] Install .Net Core SDK
The P compiler is implemented in C# and hence the tool chain requires `dotnet`.
P currently uses the specific version of [.Net SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).


=== "MacOS"

    Installing .Net SDK on MacOS using Homebrew ([details](https://formulae.brew.sh/cask/dotnet))

    ```shell
    brew tap isen-ng/dotnet-sdk-versions
    brew install --cask dotnet-sdk8-0-200
    ```

    Dont have Homebrew? :upside_down_face: Install manually using the installer for [x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.201-macos-x64-installer) or [Arm64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.201-macos-arm64-installer).

=== "Ubuntu"

    Installing .Net SDK on Ubuntu ([details](https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu))

    ```shell
    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    ```

    ```shell
    sudo apt update && sudo apt install -y dotnet-sdk-8.0
    ```


=== "Amazon Linux"

    Installing .Net SDK on Amazon Linux 2 ([details](https://learn.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#scripted-install))

    ```shell
    wget https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh  -c 8.0 -i ~/.dotnet

    # If using a bash shell, replace .zshrc with .bashrc in the below commands
    echo 'PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH' >> ~/.zshrc
    echo 'export PATH' >> ~/.zshrc
    source ~/.zshrc

    sudo mkdir /usr/share/dotnet/
    sudo cp -r ~/.dotnet/* /usr/share/dotnet/
    ```

=== "Windows"

    Installing .Net SDK on Windows using the installer for [x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.201-windows-x64-installer) or [Arm64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.201-windows-arm64-installer)

??? hint "Troubleshoot: Confirm that dotnet is correctly installed on your machine."
    ```shell
    dotnet --list-sdks
    ```

    You must see an SDK with `8.0.*` dotnet version installed.
    If you get `dotnet` command not found error, mostly likely, you need to add the path to dotnet in your `PATH`.

    Useful resources:

    - For Ubuntu: [fxr does not exist](https://stackoverflow.com/questions/73753672/a-fatal-error-occurred-the-folder-usr-share-dotnet-host-fxr-does-not-exist)



### [Step 2] Install Java Runtime

The P compiler also requires Java (`java` version 11 or higher).

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

    Installing Java 17 on Amazon Linux (you can use any version of java >= 11)

    ```shell
    sudo yum install -y java-17-amazon-corretto
    ```

=== "Windows"

    Installing Java on Windows ([details](https://www.java.com/en/download/help/windows_manual_download.html))

??? hint "Troubleshoot: Confirm that java is correctly installed on your machine."
    ```shell
    java -version
    ```

    If you get `java` command not found error, mostly likely, you need to add the path to `java` in your `PATH`.


[//]: # (### [Step 3] Install Maven)

[//]: # ()
[//]: # (For compiling the generated Java code, the P compiler using Maven &#40;`mvn` version 3.3 or higher&#41;.)

[//]: # ()
[//]: # (=== "MacOS")

[//]: # ()
[//]: # (    Installing Maven on MacOS using Homebrew &#40;[details]&#40;https://mkyong.com/maven/install-maven-on-mac-osx/&#41;&#41;)

[//]: # ()
[//]: # (    ```)

[//]: # (    brew install maven)

[//]: # (    ```)

[//]: # ()
[//]: # (    Dont have Homebrew? Directly use [installer]&#40;https://maven.apache.org/install.html&#41;. )

[//]: # ()
[//]: # (=== "Ubuntu")

[//]: # ()
[//]: # (    Installing Maven on Ubuntu &#40;[details]&#40;https://phoenixnap.com/kb/install-maven-on-ubuntu&#41;&#41;)

[//]: # (    )
[//]: # (    ```)

[//]: # (    sudo apt install maven)

[//]: # (    ```)

[//]: # ()
[//]: # (=== "Amazon Linux")

[//]: # ()
[//]: # (    Visit the [Maven releases]&#40;http://maven.apache.org/download.cgi&#41; page and install any Maven 3.3+ release.)

[//]: # ()
[//]: # (    Steps for installing Maven 3.8.7 on Amazon Linux &#40;you can use any version of Maven 3.3+&#41;:)

[//]: # ()
[//]: # (    ```)

[//]: # (    wget https://dlcdn.apache.org/maven/maven-3/3.8.7/binaries/apache-maven-3.8.7-bin.tar.gz)

[//]: # (    tar xfv apache-maven-3.8.7-bin.tar.gz)

[//]: # (    ```)

[//]: # (    )
[//]: # (    You might do this in your home directory, yielding a folder like `` /home/$USER/apache-maven-3.8.7 ``)

[//]: # (    )
[//]: # (    Next, install the software into your environment by adding it to your path, and by defining Maven's environment variables:)

[//]: # (    )
[//]: # (    ```)

[//]: # (    export M2_HOME=/home/$USER/apache-maven-3.8.7)

[//]: # (    export M2=$M2_HOME/bin)

[//]: # (    export PATH=$M2:$PATH)

[//]: # (    ```)

[//]: # ()
[//]: # (=== "Windows")

[//]: # ()
[//]: # (    Installing Maven on Windows &#40;[details]&#40;https://maven.apache.org/install.html&#41;&#41;)

[//]: # ()
[//]: # (??? hint "Troubleshoot: Confirm that Maven is correctly installed on your machine.")

[//]: # ()
[//]: # (    `mvn -version`)

[//]: # ()
[//]: # (    If you get `mvn` command not found error, mostly likely, you need to add the path to `$M2_HOME/bin` in your `PATH`.)


### [Step 3] Install P tool

Finally, let's install the P tool as a `dotnet tool` using the following command:

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

- For developing P programs, we recommend using [Peasy](https://marketplace.visualstudio.com/items?itemName=PLanguage.peasy-extension).

- For debugging generated C# code, we recommend using [Rider](https://www.jetbrains.com/rider/) for Mac/Linux or [Visual Studio 2019](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio) for Windows.

- For debugging generated Java code, we recommend using [IntelliJ IDEA](https://www.jetbrains.com/idea/)

!!! note ""
    Great :smile:! You are all set to compile and check your first P program :mortar_board:!
