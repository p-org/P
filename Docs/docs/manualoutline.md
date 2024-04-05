!!! tip ""
    **We recommend that you start with the [Tutorials](tutsoutline.md) to get familiar with
    the P language and its tool chain.**

??? note "P Top Level Declarations Grammar"

    ```
    topDecl:                # Top-level P Program Declarations
    | typeDecl              # UserDefinedTypeDeclaration
    | enumTypeDecl          # EnumTypeDeclaration
    | eventDecl             # EventDeclaration
    | machineDecl           # MachineDeclaration
    | specDecl              # SpecDeclaration
    | funDecl               # GlobalFunctionDeclaration
    | moduleDecl            # ModuleDeclaration
    | testDecl              # TestCaseDeclaration
    ;
    ```

A P program consists of a collection of following top-level declarations:

| Top Level Declarations                       | Description                                                                                                                             |
| :------------------------------------------- | :-------------------------------------------------------------------------------------------------------------------------------------- |
| [User Defined Types](manual/datatypes.md)    | P supports users defined types as well as foreign types (types that are defined in external language)                                   |
| [Enums](manual/datatypes.md)                 | P supports declaring enum values that can be used as int constants (update the link)                                                    |
| [Events](manual/events.md)                   | Events are used by state machines to communicate with each other                                                                        |
| [State Machines](manual/statemachines.md)    | P state machines are used to model or implement the behavior of the system                                                              |
| [Specification Monitors](manual/monitors.md) | P specification monitors are used to write the safety and liveness specifications the system must satisfy for correctness               |
| [Global Functions](manual/functions.md)      | P supports declaring global functions that can be shared across state machines and spec monitors                                        |
| [Module System](manual/modulesystem.md)      | P supports a module system for implementing and testing the system modularly by dividing it into separate components                  |
| [Test Cases](manual/testcases.md)            | P test cases help programmers to write different finite scenarios under which they would like to check the correctness of their system |

!!! Tip "Models, Specifications, Model Checking Scenario"
    A quick primer on what a model
    is, versus a specification, and model checking scenarios: (1) A specification says what
    the system should do (correctness properties). (2) A model captures the details of how the
    system does it. (3) A model checking scenario provides the finite non-deterministc
    test-harness or environment under which the model checker should check that the system
    model satisfies its specifications.
