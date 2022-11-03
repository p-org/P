## Introduction

PSym is a new checker for P models developed to complement the default P checker with the primary objective 
to avoid repetition during state-space exploration. PSym guarantees to never repeat an already-explored execution, and 
hence, can exhaustively explore all possible executions. PSym also has an inbuilt coverage tracker that reports estimated 
coverage to give measurable and continuous feedback (even when no bug is found during exploration).

## Toolflow
``` mermaid
graph LR
  Pmodel(P Model) --> Pcompiler[P Compiler]--> IR(Symbolic IR in Java) --> Psym[PSym Runtime];
  Psym[PSym Runtime] --> Result[Safe/Buggy <br/> Coverage <br/> Statistics];

  style Pcompiler fill:#FFEFD5,stroke:#FFEFD5,stroke-width:2px
  style Psym fill:#FFEFD5,stroke:#FFEFD5,stroke-width:2px
  style Result fill:#CCFF66,stroke:#CCFF66,stroke-width:2px
```

## Techniques

PSym implements a collection of configurable techniques summarized as follows:

| Technique             | Description                                                                |
|-----------------------|----------------------------------------------------------------------------|
| Search Strategy       | Configure the order in which search is performed: `astar`, `random`, `dfs` |
| Choice Selection      | Configure how a scheduling or data choice is selected: `random`, `none`    |
| Never Repeat States   | Track distinct states to avoid state revisits                              |
| Stateful Backtracking | Backtrack directly without replay                                          |
| BMC                   | Run PSym as a bounded model checker                                        |

## Preconfigured Modes

For ease of usage, PSym provides a set of preconfigured exploration modes as follows:


| Mode      | Description                                                                                                                                                                                                                 |
|-----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `default` | Explore single execution at a time <br/> Search Strategy = `astar` <br/> Choice Selection = `random` <br/> Never Repeat States = `ON` <br/> Stateful Backtracking = `ON` <br/> BMC = `OFF`                                  |
| `bmc`     | Explore all executions together symbolically as a bounded model checker <br/> Search Strategy = `N/A` <br/> Choice Selection = `N/A` <br/> Never Repeat States = `OFF` <br/> Stateful Backtracking = `N/A` <br/> BMC = `ON` |
| `fuzz`    | Explore like a random fuzzer (but never repeat an execution!) <br/> Search Strategy = `random` <br/> Choice Selection = `random` <br/> Never Repeat States = `OFF` <br/> Stateful Backtracking = `OFF` <br/> BMC = `OFF`    |

Pass the CLI argument ` --mode <option> ` to set the exploration mode.

## CLI options

| CLI Option                | Description                                                    |  Default  |
|---------------------------|----------------------------------------------------------------|:---------:|
| `` --project <string> ``  | Project name                                                   |   test    |
| `` --outdir <string> ``   | Output directory                                               |  output   |
| `` --method <string> ``   | Name of the test method to execute                             |   auto    |
| `` --time-limit <sec> ``  | Time limit in seconds. Use 0 for no limit.                     |    60     |
| `` --memory-limit <MB> `` | Memory limit in megabytes. Use 0 for no limit.                 |   auto    |
| `` --iterations <int> ``  | Number of schedules/executions to explore. Use 0 for no limit. |     0     |
| `` --max-steps <int> ``   | Max scheduling steps to be explored.                           |   1000    |
| `` --seed <int> ``        | Random seed to use for the search                              |     0     |

Pass the CLI argument ` --help ` for a detailed list of options.