The P module system allows programmers to decompose their complex system into modules to
implement and test the system compositionally. More details about the underlying theory
for the P module system (assume-guarantee style compositional reasoning) is described in
the [paper](https://ankushdesai.github.io/assets/papers/modp.pdf)

In its simplest form, a module in P is a collection of state machines. The P module system allows constructing larger modules by composing or unioning modules together. Hence, a distributed system under test which is a composition of multiple components can be constructed by composing (or unioning) modules corresponding to those components. The [P test cases](testcases.md) takes as input a module that represents the **closed**[^1] system to be validated which in turn is the union or composition of all the component modules.

[^1]: A closed system is a system where all the machines or interfaces that are created are defined or implemented in the unioned modules.

??? Note "P Modules Grammar"

    ```
    modExpr :
    | ( modExpr )						        # AnnonymousModuleExpr
    | { bindExpr (, bindExpr)* }                # PrimitiveModuleExpr
    | union modExpr (, modExpr)+		        # UnionModuleExpr
    | assert idenList in modExpr		        # AssertModuleExpr
    | iden                                      # NamedModule
    ;

    # Bind a machine to an interface

    bindExpr : (iden | iden -> iden) ;          # MachineBindExpr

    # Create a named module i.e., assign a name to a module

    namedModuleDecl : module iden = modExpr ;   # Named module declaration
    ```

### Named Module

A named module declaration simply assigns a name to a module expression.

**Syntax**: `module mName = modExpr;`

`mName` is the assiged name for the module `modExpr` where `modExpr` is any of the modules described below.

=== "Named Module"
    `module serverModule = { Server, Timer };`

    The above line assigns the name `serverModule` to a primitive module consisting of machines `Server` and `Timer`.

### Primitive Module

A primitive module is a (annonymous) collection of state machines.

**Syntax**: `{ bindExpr (, bindExpr)* }`

where `bindExpr` is a binding expression which could either be just the name of a machine `iden` or a mapping `mName -> replaceName` that maps a machine `mName` to a machine name `replaceName` that we want to replace. The binding enforces that whenever a machine `replaceName` is created in the module (i.e., `new replaceName(..)`) it leads to the creation of machine `mName`. The indirection using this binding is helpful in cases where we would like to replace a machine with another machine (e.g., implementation by its abstraction). This usecase is explained in the ClientServer example.

In most cases, a primitive module is simply a list of state machines that together implement that component.

=== "Primitive Module"

    ```
    // Lets say there are three machines in the P program: Client, Server, and Timer
    module client = { Client };
    module server = { Server, Timer };
    ```
    `client` is a primitive module consisting of the `Client` machine and the `server` module is a primitive module consistency of machines `Server` and `Timer`.

=== "Primitive Module with Bindings"

    ```
    // Lets say there are four machines in the P program: Client, Server, AbstractServer and Timer
    module client = { Client };
    module server = { Server, Timer };
    module serverAbs = {AbstractServer -> Server, Timer};
    ```
    `client` is a primitive module consisting of the `Client` machine and the `server` module is a primitive module consisting of machines `Server` and `Timer`. The module `serverAbs` represents a primitive module consisting of machines `AbstractServer` and `Timer` machines with the difference that wherever the `serverAbs` module is used the creation of machine `Server` will in turn lead to creation of the `AbstractServer` machine.

### Union Module

P supports unioning multiple modules together to create larger, more complex modules. The idea is to implement the distributed system as a collection of components (modules), test and verify these components in isolation using the abstractions of other components, and also potentially union them together to validate the entire system together as well.

**Syntax:**: `union modExpr (, modExpr)+`

`modExpr` is any P module. The union of two modules is simply a creation of a new module which is a union of the machines of the component modules.

`module system = (union client, server);`

`system` is a module which is a union of the modules `client` and `server`.

`module systemAbs = (union client, serverAbs);`

`systemAbs` is a module which is a union of the module `client` and the `serverAbs` where the `Client` machine interacts with the `AbstractServer` machine instead of the `Server` machine in the `system` module.

### Assert Monitors Module

P allows attaching monitors (or specifications) to modules. When attaching monitors to a module, the events observed by the monitors must be sent by `some` machine in the module.

!!! info ""
    The way to think about assert monitors module is that: `attaching these monitors to the module asserts (during P checker exploration) that each execution of the module satisfies the global properties specified by the monitors._

**Syntax:** `assert idenList in modExpr`

`idenList` is a comma separated list of monitor (spec machine) names that are being asserted on the executions of the module `modExpr`.

`assert AtomicitySpec, EventualResponse in TwoPhaseCommit`

The above module asserts that the executions of the module `TwoPhaseCommit` satisfy the properties specified by the monitors `AtomicitySpec` and `EventualResponse`.

!!! Note "More module constructors"
    P supports more complex module constructors like `compose`, `hide`, and `rename`. The description for these will be added later, they are mostly used for more advanced compositional reasoning.



