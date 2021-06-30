We use three examples of increasing complexity to illustrate various features of the P programming language and framework.

- Client Server: A simple client-server example where a client state machine sends requests to a server state machine that performs some local computation and sends a response back to the client.
We will use this example to understand P syntax, semantics, how to write specifications, run the model checker to find a bug, and then fix the bug.

- Two Phase Commit: Two phase commit protocol where a coordinator state machine communicates with participant state machines to ensure atomicity guarantees for transactions.
We will use this example to dive deeper into how to model non-determinism in systems, how to write more complex properties in P, run the model checker, find and fix the existing bug.

- Failure Detector: We will use this protocol as an exercise to understand how to model node failures in P.