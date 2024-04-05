A P program is typically divided into four folders (or parts):

- `PSrc`: contains all the state machines representing the implementation (model) of the
  system or protocol to be verified or tested.
- `PSpec`: contains all the specifications representing the _correctness_ properties that
  the system must satisfy.
- `PTst`: contains all the _environment_ or _test harness_ state machines that model the
  non-deterministic scenarios under which we want to check that the system model in `PSrc`
  satisfies the specifications in `PSpec`. P allows writing different model checking
  scenarios as test-cases.

- `PForeign`: P also supports interfacing with foreign languages like `Java`, `C#`, and
  `C/C++`. P allows programmers to implement a part of their protocol logic in these
  foreign languages and use them in a P program using the foreign types and functions interface ([Foreign](../manual/foriegntypesfunctions.md)).
  The `PForeign` folder contains
  all the foreign code used in the P program.

!!! Note "Recommendation"
    The folder structure described above is just a recommendation.
    The P compiler does not require any particular folder structure for a P project. The
    examples in the [Tutorials](../tutsoutline.md) use the same folder structure.

!!! Tip "Models, Specifications, Model Checking Scenario"
    A quick primer on what a model
    is, versus a specification, and model checking scenarios: (1) A specification says what
    the system should do (correctness properties). (2) A model captures the details of how the
    system does it. (3) A model checking scenario provides the finite non-deterministc
    test-harness or environment under which the model checker should check that the system
    model satisfies its specifications.
