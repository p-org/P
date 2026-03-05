## P Language Manual

!!! tip ""
    **We recommend starting with the [Tutorials](tutsoutline.md) to get familiar with the P language and its tool chain.**

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

---

A P program consists of a collection of the following top-level declarations:

| Declaration | Description |
| :---------- | :---------- |
| :material-code-braces: [**User Defined Types**](manual/datatypes.md) | User-defined types as well as foreign types (defined in external languages) |
| :material-format-list-numbered: [**Enums**](manual/datatypes.md) | Enum values that can be used as int constants |
| :material-email-outline: [**Events**](manual/events.md) | Events used by state machines to communicate with each other |
| :material-state-machine: [**State Machines**](manual/statemachines.md) | State machines that model or implement the behavior of the system |
| :material-shield-check: [**Specification Monitors**](manual/monitors.md) | Safety and liveness specifications the system must satisfy |
| :material-function: [**Global Functions**](manual/functions.md) | Functions shared across state machines and spec monitors |
| :material-puzzle: [**Module System**](manual/modulesystem.md) | Modular system design by dividing into separate components |
| :material-test-tube: [**Test Cases**](manual/testcases.md) | Finite scenarios for checking system correctness |

??? tip "Models, Specifications, and Model Checking Scenarios"
    A quick primer:

    - **Specification** — says _what_ the system should do (correctness properties)
    - **Model** — captures the details of _how_ the system does it
    - **Model checking scenario** — provides the finite non-deterministic test-harness under which the model checker verifies that the model satisfies its specifications
