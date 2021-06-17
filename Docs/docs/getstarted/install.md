## Installing P Tool Chain

The P compiler is a NuGet library and works on .NET Core which means it can be used on Windows, Linux and
macOS.

### Prerequisites
- [.NET 3.1 SDK](https://dotnet.microsoft.com/download/dotnet/3.1)

- [Java JRE](http://www.oracle.com/technetwork/java/javase/downloads/index.html) [^1]

[^1]: P compiler uses [ANTLR](https://www.antlr.org/) parser generator that using Java.

**Optional:**

- For developing P programs: [IntelliJ IDEA](https://www.jetbrains.com/idea/) as we support basic [P syntax highlighting](syntaxhighlight.md) for IntelliJ.
  
- For debugging generated C# code: [Rider](https://www.jetbrains.com/rider/) or [Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/) or [Visual Studio 2019](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio).

- For debugging generated Java code: [IntelliJ IDEA](https://www.jetbrains.com/idea/)

### Nuget Packages

<div>
<a href="https://www.nuget.org/packages/P/" class="btn btn-primary mt-20 mr-30" target="_blank">Install P Compiler</a>
<a href="https://www.nuget.org/packages/Microsoft.Coyote/1.0.5" class="btn btn-primary mt-20 mr-30" target="_blank">Install Coyote Version 1.0.5</a>
<br/>
</div>

### Installing the P compiler

Install the `dotnet tool` named `P` using the following command:

```shell
dotnet tool install --global P
```

Now you can run the `P` compiler without having to build P from source. Type `pc
-h` to see if it is working.

You can update the version of `P` tool by running the following command:

```shell
dotnet tool update --global P
```

You can remove the global `P` tool by running the following command:

```shell
dotnet tool uninstall --global P
```

### Installing the P Checker
The current P concurrency checker depends on [Coyote](https://microsoft.github.io/coyote/) (previously [P#](https://github.com/p-org/PSharp))

Install the `Coyote` version `1.0.5` using the following command:

```shell
dotnet tool install --global Microsoft.Coyote.CLI --version 1.0.5
```

Now you can run `coyote` to check the correctness of P programs. Type `coyote --help` to see if it is working.

**On MacOS or Linux**

We recommend that you add the following alias to the bash profile (`~/.bash_profile`) so that you can invoke the P checker (pmc) from the commandline.

Add the following line to the `~/.bash_profile`.

```shell
alias pmc='coyote test'
```
**On Windows**

We recommend that you add the following to the `Microsoft.PowerShell_profile` normally found in `D:\Users\<username>\Documents\WindowsPowerShell`

```shell
function pmc { coyote test $args}
```

### Troubleshooting

#### [Error] `pc` or `coyote` command not found after installation

After installation, run `which pc` (or `which coyote`) and it should show:

```shell
which pc
/Users/<user>/.dotnet/tools/pc
```

If not, add `$HOME/.dotnet/tools` to `$PATH` in your `.bash_profile` (or equivalent) and try again after restarting the shell.

### Using the P tool
Great! You are all set to compile and test your first P program. To learn how to use the P tool chain read [here](clientserver.md).
