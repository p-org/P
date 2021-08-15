!!! tip ""  
    **We recommend that you start with the [Tutorials](tutsoutline.md) to get
    familiar with the P language and its tool chain.**


??? note "P Top Level Declarations Grammar"

    ```
    topDecl:                # Top-level P Program Declarations
    | TypeDefDecl           # UserDefinedTypeDeclaration
    | enumTypeDecl          # EnumTypeDeclaration
    | eventDecl             # EventDeclaration
    | interfaceDecl         # InterfaceDeclaration
    | MachineDecl           # MachineDeclaration
    | specDecl              # SpecDeclaration
    | funDecl               # GlobalFunctionDeclaration
    | ModuleDecl            # ModuleDeclaration
    | testDecl              # TestCaseDeclaration
    ;
    ```

A P program consists of a collection of the following high-level declarations:

| Top Level Declarations |                                                               Description                                                               |              Details Link               |
|:----------------------:|:---------------------------------------------------------------------------------------------------------------------------------------:|:---------------------------------------:|
|   User Defined Types   |                  P supports users defined types as well as foreign types (types that are defined in external language)                  |    [DataTypes](manual/datatypes.md)     |
|         Enums          |                          P supports declaring enum values that can be used as int constants (update the link)                           |    [DataTypes](manual/datatypes.md)     |
|         Events         |                                    Events are used by state machines to communicate with each other                                     |       [Events](manual/events.md)        |
|       Interfaces       |                    Each machine in P implements an interface specifying the events the machine is willing to receive                    |    [Interfaces](manual/interface.md)    |
|     State Machines     |                               P state machines are used to model or implement the behavior of the system                                |  [State Machines](manual/machines.md)   |
| Specification Monitors |        P specification monitors are used to write the safety and liveness specifications the system must satisfy for correctness        |   [Spec Monitors](manual/monitors.md)   |
|    Global Functions    |                    P supports declaring global functions that can be shared across state machines and spec monitors                     | [Global Functions](manual/functions.md) |
|     Module System      |         P supports a module system for implementing and testing the system modularly by dividing them into separate components          | [Module System](manual/modulesystem.md) |
|       Test Cases       | P test cases helps programmers to write different finite scenarios under which they would like to check the correctness of their system |    [Test Cases](manual/testcases.md)    |

!!! Tip "Models, Specifications, Model Checking Scenario"
    A quick primer on what a model
    is, versus a specification, and model checking scenarios: (1) A specification says what
    the system should do (correctness properties). (2) A model captures the details of how the
    system does it. (3) A model checking scenario provides the finite non-deterministc
    test-harness or environment under which the model checker should check that the system
    model satisfies its specifications.