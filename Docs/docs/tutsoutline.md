
!!! tip "P Language Semantics"
    Before we get started with the tutorials, please read [{==this==}](tutorial/psemantics.md) to get an informal overview of the P language semantics.

In this tutorial, we use a series of examples along with excercise problems to help you get familiar with the P language and the associated tool chain.

!!! check "How to use this tutorial"

    We recommend that you work through these examples one-by-one by solving the accompanying exercise problems before moving to the next example. If you have any doubts or questions, **please feel free to ask them in [discussions](https://github.com/p-org/P/discussions/categories/q-a) or ping (email) Ankush Desai**.

-----

### [Example 1] **Client Server**

A simple client-server example where a client sends requests to withdraw money from a bank server. The bank server uses a database service to store the clients bank balance and on receiving a withdraw request updates the database and responds back to the client wit the new balance in the account. In this simple example, we would like to ensure that the bank never lies about the bank balance for a client and the withdraw request always succeeds if there is enough balance in the account. We will use this example to understand P syntax, semantics, how to write specifications, run the checker to find a bug, and then fix the bug.

!!! summary "Summary"
    In this example, you will learn about P state machines, writing simple safety and liveness specifications as P monitors, and finally writing multiple model checking scenarios to check the correctness of the P program.

-----

_Now that we understand the basic features of the P language, lets spice things up by looking at a distributed protocol, and the first choice is to start with the textbook example of a two phase commit protocol!_

-----

### [Example 2] **Two Phase Commit**

Two phase commit protocol where a coordinator state machine communicates with participant state machines to ensure atomicity guarantees for transactions.
We will use this example to dive deeper into how to model non-determinism in systems, how to write more complex properties in P, run the model checker, find and fix the existing bug.

-----

_Wow, we have reached the middle of our tutorials, its time to take a break and have an espresso coffee! In the next example, instead of modeling a distributed protocol, we consider the fun example of modeling an espresso machine and see how we can use the P state machine to model a reactive system that must respond correctly to user inputs._

-----

### [Example 3] **Espresso Machine**

!!! summary "Summary"
    In this example, you will learn about P state machines, writing simple safety and liveness specifications as P monitors, and finally writing multiple model checking scenarios to check the correctness of the P program.

-----

### [Example 4] **Failure Detector**

!!! summary "Summary"
    In this example, you will learn about P state machines, writing simple safety and liveness specifications as P monitors, and finally writing multiple model checking scenarios to check the correctness of the P program.

-----

### [Example 5] **Simple Paxos**

!!! summary "Summary"
    In this example, you will learn about P state machines, writing simple safety and liveness specifications as P monitors, and finally writing multiple model checking scenarios to check the correctness of the P program.

-----

### [Common] **Timer, Failure Injector, and Shared Memory**

We also describe how to model system's interaction with an OS Timer [Timer](https://github.com/p-org/P/blob/master/Tutorial/Common/Timer/), and how to model injecting node failures in the system [Failure Injector](). These models are used in the Two Phase Commit, Espresso Machine, and Failure Detector models. 

P is a purely messaging passing based programming language and hence does not support primitives for modeling shared memory based concurrency. But one can always model shared memory concurrency using message passing. We have used this style of modeling when checking the correctness of single node file systems. Please check out [Shared Memory] project on how to model shared memory concurrency using P.

-----

:face_with_cowboy_hat: :face_with_cowboy_hat: **Alright, alright, alright ... lets go!** :woman_technologist:


