PSym is built to be cross-platform and can be used on MacOS, Linux, and Windows.

### [Steps 1 to 5] Install P
Follow the instructions on installing P from [Installing P](../../getstarted/install.md)
(at least up to and including [Step 3](../../getstarted/install.md#step-3-install-p-compiler))

### [Step 6] Install Maven

If you already have Maven 3.3+ installed :innocent:, ignore this step.
To install Maven use:

=== "MacOS"

    Installing Maven on MacOS using Homebrew ([details](https://mkyong.com/maven/install-maven-on-mac-osx/))

    ```
    brew install maven
    ```

    Dont have Homebrew? Directly use [installer](https://maven.apache.org/install.html).

=== "Ubuntu"

    Installing Maven on Ubuntu ([details](https://phoenixnap.com/kb/install-maven-on-ubuntu))

    ```
    sudo apt install maven
    ```

=== "Amazon Linux"

    Visit the [Maven releases](http://maven.apache.org/download.cgi) page

    Identify the latest release or the release you wish to use.


    Steps for installing Maven 3.8.6 on Amazon Linux (you can use any version of Maven 3.3+):

    ```
    wget https://dlcdn.apache.org/maven/maven-3/3.8.6/binaries/apache-maven-3.8.6-bin.tar.gz
    tar xfv apache-maven-3.8.6-bin.tar.gz
    ```

    You might do this in your home directory, yielding a folder like `` /home/$USER/apache-maven-3.8.6 ``

    Next, install the software into your environment by adding it to your path, and by defining Maven's environment variables:

    ```
    export M2_HOME=/home/$USER/apache-maven-3.6.3
    export M2=$M2_HOME/bin
    export PATH=$M2:$PATH
    ```

=== "Windows"

    Installing Maven on Windows ([details](https://maven.apache.org/install.html))

??? hint "Troubleshoot: Confirm that Maven is correctly installed on your machine."

    `mvn -version`

    If you get `mvn` command not found error, mostly likely, you need to add the path to `$M2_HOME/bin` in your `PATH`.


Great :smile:! You are all set to check your P program with PSym :mortar_board:!
