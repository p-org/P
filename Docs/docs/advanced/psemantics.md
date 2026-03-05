## P Language Semantics (Informal)

!!! tip ""
    Before getting started with the [tutorials](../tutsoutline.md), we provide a quick informal overview of the P language semantics so that readers can keep it in mind as they walk through the tutorials and the [language manual](../manualoutline.md).

---

### P is a Programming Language

P is a state machine based _programming language_ and hence, just like any other imperative programming language it supports basic [data types](../manual/datatypes.md), [expressions](../manual/expressions.md), and [statements](../manual/statemachines.md) that enable programmers to capture complex distributed systems protocol logic as a collection of event-handlers or [functions](../manual/functions.md) (in P state machines).

---

### P State Machines

The underlying model of computation for P state machines is similar to that of [Gul Agha's](http://osl.cs.illinois.edu/members/agha.html) [Actor-model-of-computation](https://dspace.mit.edu/handle/1721.1/6952) ([wiki](https://en.wikipedia.org/wiki/Actor_model)). A P program is a collection of concurrently executing state machines that communicate with each other by sending events (or messages) asynchronously. Each P state machine has an **unbounded FIFO buffer** associated with it.

Sends are **asynchronous**, i.e., executing a send operation `send t,e,v;` adds event `e` with payload value `v` into the FIFO buffer of the target machine `t`.
Each state in the P state machine has an entry function associated with it which gets executed when the state machine enters that state. After executing the entry function, the machine tries to dequeue an event from the input buffer or blocks if the buffer is empty. Upon dequeuing an event from the input queue of the machine, the attached handler is executed which might transition the machine to a different state.

!!! note "Formal Semantics"
    For detailed formal semantics of P state machines, see the [original P paper](https://ankushdesai.github.io/assets/papers/p.pdf) and the [more recent paper](https://ankushdesai.github.io/assets/papers/modp.pdf) with updated semantics.

There are **two main distinctions** with the actor model of computation:

1. P adds the **syntactic sugar** of state machines to actors
2. Each state machine in P has an **unbounded FIFO buffer** associated with it instead of an unbounded bag in actors (semantic difference)

---

### Key Semantics

!!! danger "Send Semantics in P"
    Sends are **reliable, buffered, non-blocking, and directed** (not broadcast).

    - **Reliable** — executing a send operation adds an event into the target machine's buffer. To model message loss, it must be modeled explicitly (discussed in the Failure Detector tutorial).
    - **FIFO ordered** — events are dequeued in the **causal order** in which they were sent. Events from two different concurrent machines will be interleaved by the checker, but events from the same machine always appear in the same order.
    - **No implicit re-ordering** — arbitrary message re-ordering must be explicitly modeled in P.

    To check system correctness against an arbitrary network (with message duplicates, loss, re-order, etc.), model the corresponding send semantics in P explicitly. See the [Paxos tutorial](../tutorial/paxos.md) for an example of modeling unreliable networks with message loss and duplication.

!!! danger "New Semantics in P"
    State machines in P can be dynamically created during execution using the [`new`](../manual/statements.md#new) primitive. Creation of a state machine is also an **asynchronous, non-blocking** operation.

---

### P Monitors

Specifications in P are written as **global runtime monitors**. These monitors observe the execution of the system and can assert any global safety or liveness invariants. Monitors are synchronously composed with the P state machines. Details are explained in the [language manual](../manual/monitors.md) and in the tutorials.

!!! warning "Always specify both safety and liveness specifications"
    Only specifying safety properties is not enough — a system model may be incorrect and in the worst case drop all requests without performing any operation. Such a system trivially satisfies all safety specifications! Combining safety with **liveness properties** ensures the system is making progress and servicing requests.

---

### P Checker

The P Checker explores different possible behaviors of the P program arising from:

1. **Concurrency** — different interleavings of events from concurrently executing state machines
2. **Data nondeterminism** — different data input choices modeled using the [`choose`](../manual/expressions.md#choose) operation

The checker asserts that for each explored execution, the system satisfies the desired properties specified by the P Monitors.

---

### PObserve

[PObserve](pobserve/pobserve.md) enables validating system correctness against formal specifications using service logs. While formal specifications help catch critical bugs during the design phase, PObserve extends this capability to implementation, testing, and production phases. By operating directly on service logs without requiring additional instrumentation, PObserve allows developers to verify if their running systems satisfy the same formal correctness specifications that were validated during design.
