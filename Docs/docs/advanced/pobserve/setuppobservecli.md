# Setup and Build PObserve

### [Step 1] Install Java

PObserve is built in Java and requires Java version 17 or higher. If you already have Java 17+ installed :innocent:, ignore this step.

=== "MacOS"

    Installing Java 17 on MacOS using Homebrew ([details](https://mkyong.com/java/how-to-install-java-on-mac-osx/))
    ```
    brew install openjdk@17
    ```
    Dont have Homebrew? Directly use [installer](https://www.java.com/en/download/help/mac_install.html).

=== "Ubuntu"

    Installing Java 17 on Ubuntu ([details](https://ubuntu.com/tutorials/install-jre#2-installing-openjdk-jre))

    ```
    sudo apt update
    sudo apt install openjdk-17-jdk
    ```

=== "Amazon Linux"

    Installing Java 17 on Amazon Linux:

    ```
    sudo yum install java-17-amazon-corretto-devel
    ```

=== "Windows"

    Installing Java 17 on Windows:
    
    1. Download Java 17 from [Oracle](https://www.oracle.com/java/technologies/downloads/) or [OpenJDK](https://openjdk.org/install/)
    2. Run the installer and follow the setup wizard
    3. Add Java to your PATH environment variable

??? hint "Troubleshoot: Confirm that Java 17+ is correctly installed on your machine."
    `java -version`

    You should see output showing Java version 17 or higher. If you get `java` command not found error, you most likely need to add the path to `java` in your `PATH` environment variable.

### [Step 2] Install Gradle

PObserve uses gradle build system and requires gradle 8.x. If you already have the required version of gradle installed, ignore this step.

=== "MacOS"

    Installing Gradle 8.x on MacOS using Homebrew ([details](https://gradle.org/install/))
    ```
    brew install gradle@8
    ```
    
    Or using SDKMAN to install specific version:
    ```
    curl -s "https://get.sdkman.io" | bash
    source "$HOME/.sdkman/bin/sdkman-init.sh"
    sdk install gradle 8.10.2
    ```

=== "Ubuntu"

    Installing Gradle 8.x on Ubuntu using SDKMAN ([details](https://gradle.org/install/))

    ```
    curl -s "https://get.sdkman.io" | bash
    source "$HOME/.sdkman/bin/sdkman-init.sh"
    sdk install gradle 8.10.2
    ```
    
    Note: The apt package may not have Gradle 8.x, so SDKMAN is recommended for version control.

=== "Amazon Linux"

    Installing Gradle 8.x on Amazon Linux using SDKMAN:

    ```
    curl -s "https://get.sdkman.io" | bash
    source "$HOME/.sdkman/bin/sdkman-init.sh"
    sdk install gradle 8.10.2
    ```

=== "Windows"

    Installing Gradle 8.x on Windows using Chocolatey ([details](https://gradle.org/install/))
    ```
    choco install gradle --version=8.10.2
    ```
    
    Or download Gradle 8.x directly from [Gradle releases](https://gradle.org/releases/) and add to PATH.

??? hint "Troubleshoot: Confirm that gradle 8.x is correctly installed on your machine."
    `gradle --version`

    You should see output showing Gradle version 8.x. If you get `gradle` command not found error, you need to add gradle to your `PATH` or use the gradle wrapper (gradlew) which will be set up in the next step.

### [Step 3] Build PObserve

1. Clone the repository
   ```bash
   git clone https://github.com/p-org/P.git
   ```

2. Generate the gradle wrapper
   ```bash
   cd P/Src/PObserve/PObserve
   gradle wrapper
   ```

3. Build the PObserve project:

    === "Linux/MacOS"

        ```bash
        ./gradlew build
        ```

    === "Windows"

        ```shell
        gradlew.bat build
        ```

The build will create a standalone JAR file with all dependencies included in path: `Src/PObserve/PObserve/build/libs/PObserve-1.0.0.jar`

!!! success ""
    :tada: **Voilà!** You now have PObserve successfully built and ready to use! 🚀

    Now that you have the PObserve JAR file, read the [PObserve CLI Guide](./pobservecli.md) page to see how to use PObserve to monitor your system logs against P specifications. Happy monitoring! 📊✨
