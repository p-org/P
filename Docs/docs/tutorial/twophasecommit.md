Now that we understand the basic features of the P language, lets look at modeling and analysis of a distributed system :man_juggling:!


**System:** We use a simplified version of the [classic two phase commit protocol](https://s2.smu.edu/~mhd/8330f11/p133-gray.pdf) to model a transaction commit service. The two phase commit protocol uses a single coordinator to gain consensus for a transaction spanning across multiple participants. A transaction in our case is simply a `write` operation for a key-value data store where the data store is replicated across participants, in other words, a `write` transaction must be committed only if its accepted by all the participant replicas and aborted if any one of the participant replica rejects the `write` request.

![Placeholder](twophasecommit.png){ align=center }

A two phase commit protocol consists of two phases :laughing: (figure above). On receiving a write transaction, the coordinator starts the first phase in which it sends a `prepare` request to all the participants and waits for a `prepare success` or `prepare failure` response. On receiving prepare response from all the participants, the coordinator moves the second phase where it sends a `commit` or `abort` message to the participants and also responds back to the client if the write transaction was committed or aborted by the service. 

**Assumptions:** Note that our transaction commit system is ridiculously simplified, for example, (1) it does allow multiple concurrent clients to issue transactions in parallel but the coordinator serializes these transaction and services them one-by-one, (2) it is not fault tolerant to node failures, failure of either coordinator or any of the participants will block the progress forever. Also, we rely on [P's reliable send semantics](../advanced/psemantics.md) to model the behavior of the underlying network, hence, our models assume reliable delivery of messages.

**Correctness Specification:** We would like our transaction commit service to provide atomicity guarantees for each transaction, i.e., if the service responds to the client that a transaction was committed then that transaction must have been committed by each of its participants and if a transaction is aborted then atleast one of the participant must have rejected the transaction. We would also like to check that under the assumptions above (no node failures and reliable network), each transaction request is eventually responded by the transaction commit service.

!!! summary "Summary" 
    We will use this example to dive deeper into: (1) modeling non-determinism in the systems, in particular, time-outs (2) writing complex safety properties like atomicity of transactions in P, and finally, modeling node failures in P using a failure injector state machine. We also use a very simple example to show how P allows invoking foreign code from the P programs. More details in [P foreign interface](../manual/foriegntypesfunctions.md).

