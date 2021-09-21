To get familiar with the P langauage and the associated tool chain, we provide a series of examples along with excercise problems.

!!! tip "P Language Semantics"
    Before getting started with the Tutorials, please read through [this](tutorial/psemantics.md) to get a quick overview of the P language semantics.

!!! check "How to use this tutorials"

    We recommend to work through the examples in the tutorials one-by-one by solving the accompanying exercise problems before moving to the next example.

- **Client Server:** A simple client-server example where a client sends requests to withdraw money from a bank server. The bank server uses a database service to store the clients bank balance and on receiving a withdraw request updates the database and responds back to the client wit the new balance in the account. In this simple example, we would like to ensure that the bank never lies about the bank balance for a client and the withdraw request always succeeds if there is enough balance in the account. We will use this example to understand P syntax, semantics, how to write specifications, run the checker to find a bug, and then fix the bug.

Now that we understand the basic features of the P language, lets try to model a distributed protocol in P and the first choice is to start with the well known two phase commit protocol!

- **Two Phase Commit:** Two phase commit protocol where a coordinator state machine communicates with participant state machines to ensure atomicity guarantees for transactions.
We will use this example to dive deeper into how to model non-determinism in systems, how to write more complex properties in P, run the model checker, find and fix the existing bug.

Now, we have reached the middle of our tutorials and its time to take a break and have some espresso coffee! In the next example, instead of modeling a distributed protocol, we consider the fun example of modeling an espresso machine and see how we can use the P state machine to model its desired behavior.

- **Espresso Machine:**

- **Failure Detector:** We will use this protocol as an exercise to understand how to model node failures in P.

- **Simple Paxos:**