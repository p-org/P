## Structure of a P Program

A P program is typically divided into four folders:

| Folder | Purpose |
|--------|---------|
| **`PSrc/`** | State machines representing the implementation (model) of the system or protocol to be verified |
| **`PSpec/`** | Specifications representing the _correctness_ properties the system must satisfy |
| **`PTst/`** | Environment or test harness state machines that model non-deterministic scenarios for checking the system |
| **`PForeign/`** | Foreign code (Java, C#, C/C++) used via the [P foreign interface](../manual/foreigntypesfunctions.md) |

!!! note "Recommendation"
    The folder structure described above is just a recommendation. The P compiler does not require any particular folder structure for a P project. The examples in the [Tutorials](../tutsoutline.md) use the same folder structure.

??? tip "Models, Specifications, and Model Checking Scenarios"
    A quick primer:

    - **Specification** — says _what_ the system should do (correctness properties)
    - **Model** — captures the details of _how_ the system does it
    - **Model checking scenario** — provides the finite non-deterministic test-harness under which the model checker verifies that the model satisfies its specifications
