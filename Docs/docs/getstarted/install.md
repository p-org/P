## Installing P

!!! info "Looking for P 1.x?"
    If you want to use older P version 1.x.x, please use the installation steps [here](../old/getstarted/install.md).

P is built to be **cross-platform** and can be used on MacOS, Linux, and Windows. Follow the steps below to install P along with the required dependencies.

!!! success "Verify each step"
    After each step, use the troubleshooting check to ensure the installation succeeded.

!!! tip "Prefer Docker? Skip the manual setup"
    If you just want to try P, or you are running in CI, you can use the
    official Docker image instead of installing the toolchain by hand. It
    bundles the .NET SDK, JDK, Maven, graphviz, and the `p` CLI. Jump to
    [Alternative: Use the Docker image](#alternative-use-the-docker-image).

---

### :material-numeric-1-circle:{ .lg } Install .NET SDK
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



---

### :material-numeric-2-circle:{ .lg } Install Java Runtime

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


---

### :material-numeric-3-circle:{ .lg } Install P Tool

Finally, install the P tool as a `dotnet tool`:

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

---

## Alternative: Use the Docker image

Instead of installing the .NET SDK, Java, and the `p` tool separately, you can
use the official P Docker image. This is the fastest way to get a working P
toolchain, and is especially convenient for **CI pipelines** and for trying P
without changing your machine's setup.

The image is published to the GitHub Container Registry (GHCR) and is built for
both `linux/amd64` and `linux/arm64`.

!!! info "Prerequisite"
    You need [Docker](https://docs.docker.com/get-docker/) (or a compatible
    runtime such as Finch/Podman) installed.

### Pull the image

```shell
docker pull ghcr.io/p-org/p:latest
```

You can also pin a specific version, e.g. `ghcr.io/p-org/p:2.2.6`.

### Compile and check a P project

Mount your P project into the container's `/workspace` directory and run the
`p` commands as usual:

```shell
# From the root of your P project (where the .pproj file lives)
docker run --rm -it -v "$PWD":/workspace ghcr.io/p-org/p:latest bash

# Now, inside the container:
p compile
p check -tc <testcase> -i 1000
```

Anything the tool writes (e.g. `PGenerated/`, `PCheckerOutput/`) lands back in
your project directory on the host, because it is a mounted volume.

You can also run a one-shot command without an interactive shell:

```shell
docker run --rm -v "$PWD":/workspace ghcr.io/p-org/p:latest p compile
```

??? hint "Troubleshoot: verify the toolchain inside the image"
    ```shell
    docker run --rm ghcr.io/p-org/p:latest bash -c 'p --version && java -version'
    ```

    You should see the P version and a Java 17 runtime.

---

### :material-numeric-4-circle:{ .lg } Recommended IDE (Optional)

| Purpose | Recommended Tool |
|---------|-----------------|
| Developing P programs | [**Peasy**](https://marketplace.visualstudio.com/items?itemName=PLanguage.peasy-extension) (VS Code extension) |
| AI-assisted P development | [**PeasyAI**](peasyai.md) (Cursor / Claude Code) |

!!! success ""
    Great :smile:! You are all set to compile and check your first P program :mortar_board:!
