# P Model Checker Portfolio Runner

P Model Checker supports several backends and search strategies to explore the state space of the system. There is no one strategy that works best across -- bug-finding, or better coverage, or for proof (exhaustive search). Hence, the portfolio runner provides a simple script to run all the different search techniques to the best results for bug-finding, state-space coverage, and exhaustive search for proof.

Let's get started with the Portfolio Runner. The portfolio runner runs the P checker in parallel.

&nbsp;
## Prerequisites
&nbsp;
### Install P
Follow the installation steps on https://p-org.github.io/P/getstarted/install/


&nbsp;
### Portfolio Configuration File

First, you will need to add a configuration file named `portfolio-config.json` in your P project directory. This file will  contain the needed information to configure the portfolio run.

Note that `portfolio-config.json` should be present at the top level of the P project directory, i.e., same directory as `<project-name>.pproj` file.

Checkout an example `portfolio-config.json` file [here](../../../Tutorial/1_ClientServer/portfolio-config.json).

The following parameters are currently supported in `portfolio-config.json`:

|   **Parameter**    | **Description**                                                                     |                            **Recommended**                            |
|:------------------:|-------------------------------------------------------------------------------------|:---------------------------------------------------------------------:|
|      "pproj"       | Name of the `.pproj` file in the project directory                                  |                        `<project-name>.pproj`                         |
|       "dll"        | Path to .dll file, relative to project directory                                    |             `PGenerated/CSharp/net6.0/<project-name>.dll`             |
|    "partitions"    | Number of checker runs for parallel analysis, per method                            |                                `1000`                                 |
|     "timeout"      | Timeout in seconds, per method per partition                                        |                                `28800`                                |
|  "schedules"       | Number of schedules, per method per partition                                       |                               `100000`                                |
|    "max-steps"     | Number of scheduling points to explore in each model execution explored by checker  |                                `10000`                                |
|     "methods"      | Suffixes of test methods to execute                                                 |               `[comma-separated list of method names]`                |
| "polling-interval" | How frequently to check for job completion, in seconds                              |                                 `10`                                  |
|     "verbose"      | Enable/disable verbose output                                                       |                                `false`                                |
|     "psym-jar"     | Path to PSym .jar file, relative to project directory                               | `PGenerated/Symbolic/target/<project-name>-jar-with-dependencies.jar` |


&nbsp;
## Running Portfolio Checker

Simply run the portfolio runner `pmc.py` script directly as:
```bash
python3 pmc.py -p <local-project-path>
```
where `<local-project-path>` is the path to the directory containing the P project

Check the progress of the portfolio run in file `<local-project-path>/portfolio.out` and check the output of different checker runs in `<local-project-path>/portfolio` directory.

Example:
```bash
python3 pmc.py -p ../../../Tutorial/1_ClientServer/
```
