<style>
  .md-typeset h1,
  .md-content__button {
    display: none;
  }
</style>

Distributed systems are notoriously hard to get right (i.e., guaranteeing correctness) as the
programmer needs to reason about numerous control paths resulting from the myriad
interleaving of events (or messages or failures). Unsurprisingly, programmers can easily
introduce subtle errors when designing these systems. Moreover, it is extremely
difficult to test distributed systems, as most control paths remain untested, and serious
bugs lie dormant for months or even years after deployment.

!!! info ""
    _The P programming framework takes several steps towards addressing these challenges by providing
    a unified framework for modeling, specifying, implementing, testing, and verifying complex
    distributed systems._

!!! note "Real-World Impact"
    P and its usage inside AWS was featured in a **Communications of the ACM** article discussing how formal methods are applied to ensure correctness in large-scale distributed systems. For more information on P's practical applications and impact in industry, see [Systems Correctness Practices at Amazon Web Services](https://cacm.acm.org/practice/systems-correctness-practices-at-amazon-web-services/).

P was developed as a practical approach to enable wider adoption of formal methods among software engineers. While mathematical languages like TLA+ are powerful for formal verification, many engineers find them challenging to learn and apply effectively. P addresses this gap by providing a state-machine-based language that aligns with how developers typically think about distributed systems, particularly in service-oriented architectures. Since its inception, P has been adopted by teams across major cloud services for validating protocol designs in critical systems, such as the migration of Amazon S3 from eventual to strong read-after-write consistency. By enabling engineers to model their systems as communicating state machines—a familiar mental model for many developers—P makes formal verification more accessible to software engineers without extensive background in formal methods.

### P Ecosystem (Peco)

P is an ecosystem of tools and applications that together help integrate formal methods into the development
process of service teams.

#### P Language

P provides a high-level **state machine based programming language** to formally model and specify
distributed systems. The syntactic sugar of state machines allows programmers to capture
their system design (or _protocol logic_) as communicating state machines, which is how programmers generally
think about their system's design. P is more of a programming language than a mathematical
modelling language and hence, making it easier for the programmers to both: (1) create formal models that are closer
to the implementation (sufficiently detailed) and also (2) maintain these models as the system design evolves.
We found that P being a programming language rather than a mathematical language (e.g., TLA+) has played a key role in its adoption by software developers with no prior background in formal methods.
P supports specifying and checking both safety as well as liveness specifications (global invariants).
Programmers can easily write different scenarios under which they would like to check that the system satisfies the desired correctness specification.
The P module system enables programmers to model their system _modularly_ and
perform _compositional_ testing to scale the analysis to large distributed systems.


!!! Tip "Models, Specifications, Model Checking Scenario"
    A quick primer on what a model
    is, versus a specification, and model checking scenarios: (1) a specification says what
    the system should do (correctness properties); (2) a model captures the details of how the
    system does it; (3) a model checking scenario provides the finite non-deterministc
    test-harness or environment under which the model checker should check that the system
    model satisfies its specifications.

The underlying model of computation for P programs is communicating state machines (or [actors](https://en.wikipedia.org/wiki/Actor_model)). The detailed formal semantics for P can be found [here](https://ankushdesai.github.io/assets/papers/modp.pdf) and an informal discussion [here](advanced/psemantics.md).

#### PChecker - Backend Analysis Engines

P provides backend analysis engines to systematically explore behaviors of the system model (_resulting from interleaving of messages and failures_) and check that the model satisfies the desired _correctness_ specifications.
To reason about complex distributed systems, the P checker needs to tackle the well-known problem of _state space explosion_. The P checker currently leverages two approaches for validation:

1. **Coverage guided exploration for efficient bug finding:** The P checker employs [search prioritization heuristics](https://ankushdesai.github.io/assets/papers/fse-desai.pdf) to drive the exploration along different parts of the state space that are most likely to have concurrency related issues. This approach is really **efficient at uncovering deep bugs** (i.e., bugs that require complex interleaving of events) in the system design that have a really low probability of occurrence in real-world. On finding a bug, the checker provides a reproducible error-trace which the programmer can use for debugging.

2. **Exhaustive model checking:** The P checker also performs exhaustive search based on model-checking to verify P programs for small (finite) instances of the system (i.e., finite inputs and finite processes in the system).

Although the current P checker is great at finding _deep-hard-to-find_ bugs ("[Heisenbugs](https://en.wikipedia.org/wiki/Heisenbug)"), it **cannot provide a proof** of correctness for unbounded instances.
We are actively working on addressing this challenge and are building a deductive verification engine to perform **mathematical proof** of correctness for P programs. 

#### PObserve - Runtime Monitoring with Formal Specifications

PObserve is a scalable distributed runtime monitoring framework that allows checking formal correctness specifications on systems implementation using service logs. As teams use P to validate correctness of their system design, the key question that is often raised is: "Design validation is super valuable, but, its the implementation that gets shipped (and not design), how do we connect our design-level specifications to implementation?" 

PObserve bridges this gap by allowing teams to continuously monitor if their system implementation satisfies the design-level correctness specification throughout the development lifecycle: during testing, in pre-production environments, and in production systems. This provides a continuous connection between the formal models used for design validation and the actual running implementation.

#### Peasy - An Intuitive Development Environment for P

Peasy is a VS Code language extension for P that provides a comprehensive development environment. It offers a rich set of features including syntax highlighting, compilation, error reporting, and unit testing of P formal models within the VS Code environment. Peasy also includes powerful visualization tools for state machines and error traces, which aid in development and debugging of counter examples provided by the P checker. These capabilities make P development more accessible and provide valuable visual aids for understanding complex state machine behaviors.

If you are wondering "why do formal methods at all?" or "how are organizations using P to gain confidence in correctness of their services?", the following re:Invent 2023 talk answers this question, provides an overview of P, and its impact:
[Re:Invent 2023 Talk: Gain confidence in system correctness & resilience with Formal Methods (Finding Critical Bugs Early!!)](https://www.youtube.com/watch?v=YTIrGzD5Yc4)
