
!!! tip "P Language Semantics"
    Before we get started with the tutorials, please read [{==this==}](advanced/psemantics.md) to get an informal overview of the P language semantics.

In this tutorial, we use a series of examples along with exercise problems to help you get familiar with the P language and the associated tool chain.

!!! check "How to use this tutorial"

    We recommend that you work through these examples one-by-one by solving the accompanying exercise problems before moving to the next example. If you have any doubts or questions, **please feel free to ask them in [discussions](https://github.com/p-org/P/discussions/categories/q-a) or create an [issue](https://github.com/p-org/P/issues)**.

    Also, we assume that you have cloned the P repository locally.
    ```shell
    git clone https://github.com/p-org/P.git
    ```

    The recommended way to work through this example is to open the [P/Tutorial](https://github.com/p-org/P/tree/master/Tutorial) folder in IntelliJ side-by-side a browser using which you can simultaneously read the description for each example and browse the P program.

    To know more about P language primitives used in the examples, please look them up in the [language manual](manualoutline.md).

-----

### **[[Example 1] Client Server](tutorial/clientserver.md)**

We start with a simple client-server example consisting of clients interact with a bank server to withdraw money from their account. The bank server uses a backend database service to store the account balance for its clients. We will use this example to demonstrate how to implement such a system as a collection of P state machines and also check the correctness property that the bank always responds with the correct account balance for a client and a withdraw request always succeeds if there is enough balance in the account. We will also use P's capability to write multiple model checking scenarios and demonstrate how one can replace components in P with its abstraction.

!!! summary "What will we learn through this example?"
    We will learn about P state machines, writing simple safety and liveness specifications as P monitors, writing multiple model checking scenarios to check the correctness of a P program, and finally, replacing complex components in P with their abstractions using P's module system.

-----

Now that we understand the basic features of the P language, lets spice things up by looking at a well-known distributed protocol, and the obvious choice is to start with the textbook example of a two-phase commit protocol :man_juggling:!

-----

### **[[Example 2] Two Phase Commit](tutorial/twophasecommit.md)**

We use a simplified version of the [classic two phase commit protocol](https://s2.smu.edu/~mhd/8330f11/p133-gray.pdf) to model a transaction commit service.
The protocol uses a (single) coordinator to achieve consensus for a transaction spanning across multiple participants. A transaction in our case is simply a `put` operation for a key-value data store where the data store is replicated across participants.

**Assumptions:** Note that our transaction commit system is ridiculously simplified, for example, it is not fault tolerant to node failures, failure of either coordinator or any of the participants will block the progress forever. Also, we rely on [P's send semantics](advanced/psemantics.md) to model the behavior of the underlying network.

!!! summary "What will we learn through this example?"
    We will use this example to dive deeper into: (1) modeling non-determinism in distributed systems, in particular, time-outs (2) writing complex safety properties like atomicity of transactions in P and finally, (3) modeling node failures in P using a failure injector state machine. We will also show how P allows invoking foreign code from the P programs. More details in [P foreign interface](manual/foriegntypesfunctions.md).

-----

Wow! we have reached the middle of our tutorials :yawning_face: :yawning_face: , its time to take a break and have an espresso coffee! :coffee: :coffee:

In the next example, instead of modeling a distributed protocol, we consider the fun example of modeling an espresso machine and see how we can use P state machines to model a reactive system that must respond correctly to various user inputs.

-----

### **[[Example 3] Espresso Machine](tutorial/espressomachine.md)**

P has been used in the past to implement device drivers and robotics systems ([case studies](casestudies.md) and [publications](publications.md)). One of the many challenges in implementing these systems is that they are reactive system and hence, must handle various streams of events (inputs) appropriately depending on their current mode of operation.
In this example, we consider the example of an Espresso coffee machine where the user interacts with the coffee machine through its control panel. The control panel must correctly interprets inputs from the user and sends commands to the coffee maker. We use this example to demonstrate how using P state machine, one can capture the required reactive behavior of a coffee maker and define how it must handle different user inputs.

!!! summary "What will we learn through this example?"
    This is a just for fun example to demonstrate how to model a reactive system as a P state machine. We also show how using P monitors we can check that the system moves through the correct modes of operation.

-----

Energized with the Coffee :coffee:, lets get back to distributed systems. After the two phase commit protocol, the next protocol that we will jump to is a simple broadcast-based failure detector!
By this point in the tutorial, we have gotten familiar with the P language and most of its features. So, working through this example should be super fast!

-----

### **[[Example 4] Failure Detector](tutorial/failuredetector.md)**

 We use a broadcast based failure detector to show how to model lossy network and node failures in P. The failure detector basically broadcasts ping messages to all nodes in the system and uses a timer to wait for a pong response from all nodes. If certain node does not respond with a pong message after multiple attempts, the failure detector marks the node as down and notifies the clients. We check using a liveness specification that if the failure injecter shutsdown a particular node then the failure detector always eventually detects that node has failed and notifies the client.

!!! summary "What will we learn through this example?"
    In this example, we demonstrate how to use data nondeterminism to model message loss, unreliable sends, and node failures. We also discuss how to model other types of network nondeterminism. Finally, we give an example of a liveness specification that the failure detector must satisfy.

-----
<!---
How can we finish our tutorials on modeling distributed systems without giving tribute to the Paxos protocol (and our inspiration :pray: [Leslie Lamport](http://www.lamport.org/) :pray: ). Lets end the tutorial with a simplified **[single decree paxos](https://mwhittaker.github.io/blog/single_decree_paxos/)**.

### **[[Example 5] Single Decree Paxos](tutorial/paxos.md)**

We present a simplified model of the [single decree paxos](https://mwhittaker.github.io/blog/single_decree_paxos/). We say simplified because general paxos is resilient against arbitrary network (lossy, duplicate, re-order, and delay), in our case we only model message loss and delay, and check correctness of paxos in the presence of such a network. This is a fun exercise, we encourage you to play around and create variants of paxos!

!!! summary "What will we learn through this example?"
    In this example, we present a simplified model of the single decree paxos. (Todo: add details about the properties checked)

-----
--->

### **[[Common] Timer, Failure Injector, and Shared Memory](tutorial/common.md)**

We have described how to model system's interaction with an OS Timer [Timer](https://github.com/p-org/P/blob/master/Tutorial/Common/Timer/), and how to model injecting node failures in the system [Failure Injector](https://github.com/p-org/P/tree/master/Tutorial/Common/FailureInjector). These models are used in the Two Phase Commit, Espresso Machine, and Failure Detector examples.

P is a purely messaging passing based programming language and hence does not support primitives for modeling shared memory based concurrency. But one can always model shared memory concurrency using message passing. We have used this style of modeling when checking the correctness of single node file systems. Please check out [Shared Memory](https://github.com/p-org/P/tree/master/Tutorial/Common/SharedMemory) example for how to model shared memory concurrency using P.

-----

:face_with_cowboy_hat: :face_with_cowboy_hat: **Alright, alright, alright ... lets go!** :woman_technologist:
