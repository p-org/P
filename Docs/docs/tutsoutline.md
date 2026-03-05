## Tutorials

!!! tip "P Language Semantics"
    Before getting started, please read the [informal overview of P language semantics](advanced/psemantics.md).

In this tutorial, we use a series of examples along with exercise problems to help you get familiar with the P language and the associated tool chain.

!!! check "How to use this tutorial"

    We recommend that you work through these examples one-by-one by solving the accompanying exercise problems before moving to the next example. If you have any doubts or questions, **please feel free to ask them in [discussions](https://github.com/p-org/P/discussions/categories/q-a) or create an [issue](https://github.com/p-org/P/issues)**.

    Also, we assume that you have cloned the P repository locally:
    ```shell
    git clone https://github.com/p-org/P.git
    ```

    The recommended way to work through this example is to open the [P/Tutorial](https://github.com/p-org/P/tree/master/Tutorial) folder in [Peasy IDE](getstarted/PeasyIDE.md) (VS Code extension) or your preferred editor side-by-side with a browser, so you can simultaneously read the description for each example and browse the P program.

    To know more about P language primitives used in the examples, please look them up in the [language manual](manualoutline.md).

---

### :material-numeric-1-circle:{ .lg } [Client Server](tutorial/clientserver.md)

We start with a simple client-server example where clients interact with a bank server to withdraw money from their account. The bank server uses a backend database service to store the account balance for its clients. We use this example to demonstrate how to implement such a system as a collection of P state machines and check the correctness property that the bank always responds with the correct account balance.

!!! abstract "What will we learn?"
    P state machines, writing simple safety and liveness specifications as P monitors, writing multiple model checking scenarios, and replacing complex components with their abstractions using P's module system.

---

### :material-numeric-2-circle:{ .lg } [Two Phase Commit](tutorial/twophasecommit.md)

We use a simplified version of the [classic two phase commit protocol](https://s2.smu.edu/~mhd/8330f11/p133-gray.pdf) to model a transaction commit service. The protocol uses a (single) coordinator to achieve consensus for a transaction spanning across multiple participants.

!!! warning "Assumptions"
    Our transaction commit system is ridiculously simplified — it is not fault tolerant to node failures, and failure of either coordinator or any participant will block progress forever. We rely on [P's send semantics](advanced/psemantics.md) to model the underlying network.

!!! abstract "What will we learn?"
    Modeling non-determinism (time-outs), writing complex safety properties (atomicity of transactions), modeling node failures, and invoking foreign code from P programs via the [P foreign interface](manual/foreigntypesfunctions.md).

---

### :material-numeric-3-circle:{ .lg } [Espresso Machine](tutorial/espressomachine.md)

:coffee: Time for a break! Instead of modeling a distributed protocol, we consider the fun example of modeling an espresso machine — a reactive system that must respond correctly to various user inputs through its control panel.

!!! abstract "What will we learn?"
    Modeling a reactive system as a P state machine, and using P monitors to check that the system moves through the correct modes of operation.

---

### :material-numeric-4-circle:{ .lg } [Failure Detector](tutorial/failuredetector.md)

We use a broadcast-based failure detector to show how to model lossy networks and node failures in P. The failure detector broadcasts ping messages to all nodes and uses a timer to wait for pong responses. If a node does not respond after multiple attempts, it is marked as down and clients are notified.

!!! abstract "What will we learn?"
    Using data nondeterminism to model message loss, unreliable sends, and node failures. Modeling other types of network nondeterminism. Writing a liveness specification.

---

### :material-numeric-5-circle:{ .lg } [Single Decree Paxos](tutorial/paxos.md)

We present a simplified model of the [single decree Paxos](https://mwhittaker.github.io/blog/single_decree_paxos/) consensus protocol. Multiple proposers attempt to get a single value agreed upon by a set of acceptors, with the decided value taught to learners. We model message loss and duplication using P's nondeterminism and check that the protocol maintains the core consensus safety property: agreement.

!!! abstract "What will we learn?"
    Modeling a classic consensus protocol, using `choose()` to model unreliable networks (message loss and duplication), writing a consensus safety specification, and testing with multiple configurations.

---

### :material-puzzle:{ .lg } [Common: Timer, Failure Injector, and Shared Memory](tutorial/common.md)

Reusable building blocks used across the tutorials:

| Component | Description | Used in |
|-----------|-------------|---------|
| [**Timer**](https://github.com/p-org/P/blob/master/Tutorial/Common/Timer/) | Models system interaction with an OS timer | Two Phase Commit, Espresso Machine, Failure Detector |
| [**Failure Injector**](https://github.com/p-org/P/tree/master/Tutorial/Common/FailureInjector) | Models injecting node failures into the system | Two Phase Commit, Failure Detector |
| [**Shared Memory**](https://github.com/p-org/P/tree/master/Tutorial/Common/SharedMemory) | Models shared memory concurrency using message passing | File system examples |

!!! note ""
    P is a purely message-passing based language and does not support primitives for shared memory concurrency. However, shared memory concurrency can always be modeled using message passing.

---

:face_with_cowboy_hat: :face_with_cowboy_hat: **Alright, alright, alright ... let's go!** :woman_technologist:
