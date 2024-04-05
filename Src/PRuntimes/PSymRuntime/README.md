# PSym

[![PSym on Ubuntu](https://github.com/p-org/P/actions/workflows/psym.yml/badge.svg)](https://github.com/p-org/P/actions/workflows/psym.yml)

The basic idea behind PSym is to perform symbolic exploration of P models, powered by automated reasoning through Binary Decision Diagrams (BDDs) or SAT/SMT solvers.

PSym is composed of two components:
  1) [Compiler backend](../../PCompiler/CompilerCore/Backend/Symbolic) in C# that converts a P model to a customized symbolically-instrumented Java code.
  2) [Runtime analysis](../PSymRuntime) in Java that performs systematic symbolic exploration by executing the symbolically-instrumented Java code.

## Installing PSym
Run: `` ./scripts/build.sh ``

This installs PSym with the default BDD backend. For detailed solver options, check [SOLVERS.md](SOLVERS.md)

## Running PSym

### Usage

     ./scripts/run_psym.sh <path-to-P-project> <project-name> <args>

### Example

    ./scripts/run_psym.sh Examples/tests/PingPong/ psymExample

### Output
PSym creates a directory `` output/<project-name> `` which contains results, statistics and logs relating to the run.

    output
    └── <project-name>
        ├── compile.out             [P compiler output log]
        ├── run.out                 [PSym runtime output log]
        ├── run.err                 [PSym runtime error log]
        ├── Symbolic/*.java         [Auto-generated project symbolic IR]
        ├── Symbolic/pom.xml        [Auto-generated project POM file]
        ├── Symbolic/target/*.jar   [Auto-generated project Jar file]
        ├── output/stats*.log       [Statistics report]
        ├── output/coverage*.log    [Coverage report]
        └── plots/                  [Useful plots]

### PSym CLI Options
Check [CLI_OPTIONS.md](CLI_OPTIONS.md) for options to configure model exploration with PSym.

