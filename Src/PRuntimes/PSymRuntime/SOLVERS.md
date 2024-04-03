# Solver Backends in PSym

## Installation

## Quick Install
Run: `` ./scripts/build.sh ``

This installs PSym with the default BDD backend (powered by BDDs using the [PJBDD](https://gitlab.com/sosy-lab/software/paralleljbdd) package).

### Installing ABC
Run: `` cd scripts && ./setup_abc.sh ``

If installing ABC in a different directory, make changes to [pom.xml](https://github.com/p-org/P/blob/master/Src/PRuntimes/PSymRuntime/pom.xml) to update ``<abc.jarpath>`` and ``<abc.libpath>``

### Installing MonoSAT
Run: `` cd scripts && ./setup_monosat.sh ``

If installing MonoSAT in a different directory, make changes to [pom.xml](https://github.com/p-org/P/blob/master/Src/PRuntimes/PSymRuntime/pom.xml) to update ``<monosat.jarpath>`` and ``<monosat.libpath>``

### Installing Yices 2
Run: `` cd scripts && ./setup_yices2.sh ``

If installing Yices 2 in a different directory, make changes to [pom.xml](https://github.com/p-org/P/blob/master/Src/PRuntimes/PSymRuntime/pom.xml) to update ``<yices.jarpath>`` and ``<yices.libpath>``

### Installing Z3
Run: `` cd scripts && ./setup_z3.sh ``

If installing Z3 in a different directory, make changes to [pom.xml](https://github.com/p-org/P/blob/master/Src/PRuntimes/PSymRuntime/pom.xml) to update ``<z3.jarpath>`` and ``<z3.libpath>``


### Installing CVC5
Run: `` cd scripts && ./setup_cvc5.sh ``

If installing CVC5 in a different directory, make changes to [pom.xml](https://github.com/p-org/P/blob/master/Src/PRuntimes/PSymRuntime/pom.xml) to update ``<cvc5.jarpath>`` and ``<cvc5.libpath>``


## Switching Solver and Expression Backends
We can switch:
  1) solver type     using commandline option ``--solver`` to choose from ``bdd`` (default), ``monosat``, ``yices2``, ``z3``, or ``cvc5``
  2) expression type using commandline option ``--expr``   to choose from ``bdd`` (default), ``fraig``, ``aig``, or ``native``

### Example
Switch to MonoSAT as solver and expressions represented as FRAIGs by passing commandline options ``--solver monosat --expr fraig``

### Details
Here are some additional details on these backends:
  1) ``bdd`` is the default solver and expression type, and does not require installing ABC or any SAT/SMT solver
  2) ``aig`` stands for And-Inverter-Graphs (AIG) and requires ABC installed
  3) ``fraig`` stands for Functionally-Reduced AIG (FRAIG) and requires ABC installed
  4) ``native`` is an inbuilt expression type and does not require ABC installed
  5) ``aig``, ``fraig``, and ``native`` require the solver type to be SAT/SMT based, i.e., one of ``monosat``, ``yices2``, ``z3``, or ``cvc5``
