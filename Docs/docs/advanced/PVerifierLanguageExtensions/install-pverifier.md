# Install Instructions for Amazon Linux

PVerifier requires several dependencies to be installed. Follow the steps below to set up your environment.

!!! success ""
    After each step, please use the troubleshooting check to ensure that each installation step succeeded.

### [Step 1] Install Java 11

```sh
sudo rpm --import https://yum.corretto.aws/corretto.key
sudo curl -L-o /etc/yum.repos.d/corretto.repo https://yum.corretto.aws/corretto.repo 
sudo yum install java-11-amazon-corretto-devel
```

??? hint "Troubleshoot: Confirm that java is correctly installed on your machine."
    ```shell
    java -version
    ```

    If you get `java` command not found error, most likely, you need to add the path to `java` in your `PATH`.

### [Step 2] Install SBT

```sh
sudo rm -f /etc/yum.repos.d/bintray-rpm.repo || true
curl -L https://www.scala-sbt.org/sbt-rpm.repo > sbt-rpm.repo
sudo mv sbt-rpm.repo /etc/yum.repos.d/
sudo yum install sbt
```

??? hint "Troubleshoot: Confirm that sbt is correctly installed on your machine."
    ```shell
    sbt sbtVersion
    ```

    If you get `sbt` command not found error, most likely, you need to add the path to `sbt` in your `PATH`.

### [Step 3] Install Z3

```sh
cd ~
git clone https://github.com/Z3Prover/z3.git
cd z3
python scripts/mk_make.py --java 
cd build; make
```

Then add the following lines to your `.zshrc` (or `.bashrc` if using bash) and run `source ~/.zshrc`:

```sh
export PATH=$PATH=$HOME/z3/build/
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH=$HOME/z3/build/
```

??? hint "Troubleshoot: Confirm that Z3 is correctly installed on your machine."
    ```shell
    z3 --version
    ```

    If you get `z3` command not found error, most likely, you need to add the path to `z3` in your `PATH`.

### [Step 4] Install UCLID5

```sh
cd ~
git clone https://github.com/uclid-org/uclid.git
cd uclid
sbt update clean compile "set fork:=true" test # should fail some tests that use cvc5 and delphi 
sbt universal:packageBin
unzip target/universal/uclid-0.9.5.zip
```

Then add the following line to your `.zshrc` (or `.bashrc` if using bash) and run `source ~/.zshrc`:

```sh
export PATH=$PATH=$HOME/uclid/uclid-0.9.5/bin/
```

??? hint "Troubleshoot: Confirm that UCLID5 is correctly installed on your machine."
    ```shell
    uclid --help
    ```

    If you get `uclid` command not found error, most likely, you need to add the path to `uclid` in your `PATH`.

### [Step 5] Install PVerifier

```sh
cd ~
git clone https://github.com/p-org/P/tree/dev_p3.0/pverifier
cd P
# follow regular P install instructions
root=$(pwd)
cd $root/Bld
./build.sh
dotnet tool uninstall --global P
cd $root/Src/PCompiler/PCommandLine
dotnet pack PCommandLine.csproj --configuration Release --output ./publish -p:PackAsTool=true -p:ToolCommandName=P -p:Version=2.1.3
dotnet tool install P --global --add-source ./publish
```

??? hint "Troubleshoot: Confirm that PVerifier is correctly installed on your machine."
    ```shell
    p --version
    ```

    If you get `p` command not found error, most likely, you need to add the path to `p` in your `PATH`.