Now that we understand the basic features of the P language, lets spice things up by looking at a well known distributed protocol, and the obvious choice is to start with the textbook example of a two phase commit protocol :man_juggling:!


We use a simplified version of the [classic two phase commit protocol](https://s2.smu.edu/~mhd/8330f11/p133-gray.pdf) to model a transaction commit service. The protocol uses a single coordinator to gain consensus for a transaction spanning across multiple participants. A transaction is simply a `put` operation for a key-value data store where the data store is replicated across participants.

**Assumptions:** Note that our transaction commit system is ridiculously simplified, for example, it is not fault tolerant to node failures, failure of either coordinator or any of the participants will block the progress forever. Also, we rely on [P's send semantics](../advanced/psemantics.md) to model the behavior of the underlying network.

!!! summary "Summary"
    We will use this example to dive deeper into: (1) modeling non-determinism in the systems, in particular, time-outs (2) writing complex safety properties like atomicity of transactions in P, and finally, modeling node failures in P using a failure injector state machine. We also use a very simple example to show how P allows invoking foreign code from the P programs. More details in [P foreign interface](../manual/foriegntypesfunctions.md).

![Placeholder](twophasecommit.png){ align=center }