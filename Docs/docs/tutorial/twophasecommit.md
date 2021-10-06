??? note "How to use this example"

    We assume that you have cloned the P repository locally.
    ```shell 
    git clone https://github.com/p-org/P.git
    ```

    The recommended way to work through this example is to open the [P\Tutorial](https://github.com/p-org/P/tree/master/Tutorial) folder in IntelliJ side-by-side a browser using which you can simulatenously read the description for each example and browser the P program in IntelliJ. 

Now that we understand the basic features of the P language, lets look at modeling and analysis of a distributed system :man_juggling:!


**System:** We use a simplified version of the [classic two phase commit protocol](https://s2.smu.edu/~mhd/8330f11/p133-gray.pdf) to model a transaction commit service. The two phase commit protocol uses a single coordinator to gain consensus for a transaction spanning across multiple participants. A transaction in our case is simply a `write` operation for a key-value data store where the data store is replicated across participants, in other words, a `write` transaction must be committed only if its accepted by all the participant replicas and aborted if any one of the participant replica rejects the `write` request.

![Placeholder](twophasecommit.png){ align=center }

A two phase commit protocol consists of two phases :laughing: (figure above). On receiving a write transaction, the coordinator starts the first phase in which it sends a `prepare` request to all the participants and waits for a `prepare success` or `prepare failure` response. On receiving prepare response from all the participants, the coordinator moves the second phase where it sends a `commit` or `abort` message to the participants and also responds back to the client if the write transaction was committed or aborted by the service. 

**Assumptions:** Note that our transaction commit system is ridiculously simplified, for example, (1) it does allow multiple concurrent clients to issue transactions in parallel but the coordinator serializes these transaction and services them one-by-one, (2) it is not fault tolerant to node failures, failure of either coordinator or any of the participants will block the progress forever. Also, we rely on [P's reliable send semantics](../advanced/psemantics.md) to model the behavior of the underlying network, hence, our models assume reliable delivery of messages.

**Correctness Specification:** We would like our transaction commit service to provide atomicity guarantees for each transaction, i.e., if the service responds to the client that a transaction was committed then that transaction must have been committed by each of its participants and if a transaction is aborted then atleast one of the participant must have rejected the transaction. We would also like to check that under the assumptions above (no node failures and reliable network), each transaction request is eventually responded by the transaction commit service.

### P Project

The [1_ClientServer](https://github.com/p-org/P/tree/master/Tutorial/1_ClientServer) folder contains the source code for the [ClientServer](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/ClientServer.pproj) project. Please feel free to read details about the typical [P program structure](../advanced/structureOfPProgram.md) and [P project file](../advanced/PProject.md).

### Models

The P models ([PSrc](https://github.com/p-org/P/tree/master/Tutorial/1_ClientServer/PSrc)) for the ClientServer example consists of four files: 

1. [Client.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSrc/Client.p): Implements the Client state machine.
  
??? tip "[Expand]: Lets walk through Client.p"
    ...

- [Server.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSrc/Server.p): Implements the BankServer state machine.
  
??? tip "[Expand]: Lets walk through Server.p"
    ...

- [AbstractBankServer.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSrc/AbstractBankServer.p): Implements the AbstractBankServer state machine that provides an simplified abstraction for the BankServer machine.

??? tip "[Expand]: Lets walk through AbstractBankServer.p"
    ...

- [ClientServerModules.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSrc/ClientServerModules.p): Declares the P modules corresponding to each component in the system.

??? tip "[Expand]: Lets walk through ClientServerModules.p"
    ...

### Specifications

The P Specifications ([PSpec](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSpec)) for the ClientServer example are implemented in the [BankBalanceCorrect.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSpec/BankBalanceCorrect.p) file. We define two specifications:

- BankBalanceIsAlwaysCorrect (safety property): BankBalanceIsCorrect checks the global invariant that the account-balance communicated to the client by the bank is always correct and the banks never removes more money from the account than that withdrawn by the client! Also, if the bank denies a withdraw request then its only because the withdrawal will reduce the account balance to below 10.

- GuaranteedWithDrawProgress (liveness property): GuaranteedWithDrawProgress checks the liveness (or progress) property that all withdraw requests submitted by the client are eventually responded.

!!! info "Note" 
    BankBalanceIsCorrect also checks that if there is enough money in the account then the withdraw request must not error. Hence, the two properties above together ensure that every withdraw request if allowed will eventually succeed and the bank cannot block correct withdrawal requests.

??? tip "[Expand]: Lets walk through BankBalanceCorrect.p"
    ...

### Test Scenarios

The test scenarios folder in P consists of two parts: (1) TestDrivers: These are collection of state machines that implement the test harnesses or environment state machines for different test scenarios and (2) TestScripts: These are collection of test cases that are automatically discharged by the P checker.

The test scenarios folder for ClientServer ([PTst](https://github.com/p-org/P/tree/master/Tutorial/1_ClientServer/PTst)) consists of two files [TestDriver.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PTst/TestDriver.p) and [TestScript.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PTst/Testscript.p).

??? tip "[Expand]: Lets walk through TestDriver.p"
    ...

??? tip "[Expand]: Lets walk through TestScript.p"
    ...

### Compiling TwoPhaseCommit

### Testing TwoPhaseCommit

### Exercise Problem

!!! summary "What did we learn through this example?"
    We dived deeper into: (1) modeling non-determinism in distributed systems, in particular, time-outs (2) writing complex safety properties like atomicity of transactions in P and finally, (3) modeling node failures in P using a failure injector state machine. We will also show how P allows invoking foreign code from the P programs. More details in [P foreign interface](manual/foriegntypesfunctions.md).

