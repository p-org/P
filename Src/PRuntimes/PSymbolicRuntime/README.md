# PSym

The basic idea behind PSym is to perform symbolic exploration of P models, powered by Binary Decision Diagrams (BDDs) or SAT/SMT solvers.
PSym is composed of two components: 
  1) [Compiler backend](https://github.com/p-org/P/tree/master/Src/PCompiler/CompilerCore/Backend/Symbolic) in C# that converts a P model to a customized symbolically-instrumented Java code.
  2) [Runtime analysis](https://github.com/p-org/P/tree/master/Src/PRuntimes/PSymbolicRuntime) in Java that performs systematic symbolic exploration using the symbolically-instrumented Java code.

## Installation

### Quick Install
Run: `` cd scripts && ./build.sh ``

This installs PSym with the default backends (powered by BDDs using the [PJBDD](https://gitlab.com/sosy-lab/software/paralleljbdd) package).


### Installing ABC
Run: `` cd scripts && ./setup_abc.sh `` 

If installing ABC in a different directory, make changes to [pom.xml](https://github.com/p-org/P/blob/master/Src/PRuntimes/PSymbolicRuntime/pom.xml) to update ``<abc.jarpath>`` and ``<abc.libpath>``

### Installing Yices 2
Run: `` cd scripts && ./setup_yices2.sh `` 

If installing Yices 2 in a different directory, make changes to [pom.xml](https://github.com/p-org/P/blob/master/Src/PRuntimes/PSymbolicRuntime/pom.xml) to update ``<yices.jarpath>`` and ``<yices.libpath>``

### Installing Z3
Run: `` cd scripts && ./setup_z3.sh `` 

If installing Z3 in a different directory, make changes to [pom.xml](https://github.com/p-org/P/blob/master/Src/PRuntimes/PSymbolicRuntime/pom.xml) to update ``<z3.jarpath>`` and ``<z3.libpath>``


### Installing CVC5
Run: `` cd scripts && ./setup_cvc5.sh `` 

If installing CVC5 in a different directory, make changes to [pom.xml](https://github.com/p-org/P/blob/master/Src/PRuntimes/PSymbolicRuntime/pom.xml) to update ``<cvc5.jarpath>`` and ``<cvc5.libpath>``


## Switching Solver and Expression Backends
We can switch:
  1) solver type     using commandline option ``--solver`` to choose from ``bdd`` (default), ``yices2``, ``z3``, or ``cvc5``
  2) expression type using commandline option ``--expr``   to choose from ``bdd`` (default), ``fraig``, ``aig``, or ``native``

### Example
Switch to Yices 2 as solver and expressions represented as FRAIGs by passing commandline options ``--solver yices2 --expr fraig``

### Notes
Here are some details on these backends:
  1) ``bdd`` is the default solver and expression type, and does not require installing ABC or any SAT/SMT solver
  2) ``aig`` stands for And-Inverter-Graphs (AIG) and requires ABC installed
  3) ``fraig`` stands for Functionally-Reduced AIG (FRAIG) and requires ABC installed
  4) ``native`` is an inbuilt expression type and does not require ABC installed
  5) ``aig``, ``fraig``, and ``native`` require the solver type to be SAT/SMT based, i.e., one of ``yices2``, ``z3``, or ``cvc5``

