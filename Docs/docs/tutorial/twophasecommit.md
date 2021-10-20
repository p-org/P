??? note "How to use this example"

    We assume that you have cloned the P repository locally.
    ```shell 
    git clone https://github.com/p-org/P.git
    ```

    The recommended way to work through this example is to open the [P\Tutorial](https://github.com/p-org/P/tree/master/Tutorial) folder in IntelliJ side-by-side a browser using which you can simulatenously read the description for each example and browser the P program in IntelliJ. 

Now that we understand the basic features of the P language, lets look at modeling and analysis of a distributed system :man_juggling:!


**System:** We use a simplified version of the [classic two phase commit protocol](https://s2.smu.edu/~mhd/8330f11/p133-gray.pdf) to model a transaction commit service. The two phase commit protocol uses a coordinator to gain consensus for any transaction spanning across multiple participants. A transaction in our case is simply a `write` operation for a key-value data store where the data store is replicated across multiple participants. More concretely, a `write` transaction must be committed by the coordinator only if its accepted by all the participant replicas and must be aborted if any one of the participant replica rejects the `write` request.

![Placeholder](twophasecommit.png){ align=center }

A two phase commit protocol consists of two phases :laughing: (figure above). On receiving a write transaction, the coordinator starts the first phase in which it sends a `prepare` request to all the participants and waits for a `prepare success` or `prepare failure` response. On receiving prepare responses from all the participants, the coordinator moves to the second phase where it sends a `commit` or `abort` message to the participants and also responds back to the client.

**Assumptions:** Our transaction commit system is ridiculously simplified, just to list a few: (1) our system does allow multiple concurrent clients to issue transactions in parallel but the coordinator serializes these transaction and services them one-by-one, (2) our system is not fault tolerant to node failures, failure of either coordinator or any of the participants will block the progress forever. Also, we rely on [P's reliable send semantics](../advanced/psemantics.md) to model the behavior of the underlying network, hence, our system models assume reliable delivery of messages.

**Correctness Specification:** We would like our transaction commit service to provide atomicity guarantees for each transaction, i.e., if the service responds to the client that a transaction was committed then that transaction must have been committed by each of its participants and if a transaction is aborted then atleast one of the participant must have rejected the transaction. We would also like to check that under the assumptions above (no node failures and reliable network), each transaction request is eventually responded by the transaction commit service.

### P Project

The [2_TwoPhaseCommit](https://github.com/p-org/P/tree/master/Tutorial/2_TwoPhaseCommit) folder contains the source code for the [TwoPhaseCommit](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/TwoPhaseCommit.pproj) project.
Please feel free to read details about the recommended [P program structure](../advanced/structureOfPProgram.md) and [P project file](../advanced/PProject.md).

### Models

The P models ([PSrc](https://github.com/p-org/P/tree/master/Tutorial/2_TwoPhaseCommit/PSrc)) for the TwoPhaseCommit example consists of three files:

1. [Coordinator.p](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/PSrc/Coordinator.p): Implements the Coordinator state machine.
  
??? tip "[Expand]: Lets walk through Coordinator.p"
    ...

- [Participant.p](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/PSrc/Participant.p): Implements the Participant state machine.
  
??? tip "[Expand]: Lets walk through Participant.p"
    ...

- [TwoPhaseCommitModules.p](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/PSrc/TwoPhaseCommitModules.p): Declares the P modules corresponding to each component in the two phase commit system.

??? tip "[Expand]: Lets walk through TwoPhaseCommitModules.p"
    ...

### Timer and Failure Injector

Our two phase commit project dependends on two other projects:

- **OS Timer:** The coordinator machine uses a timer to wait for prepare responses from all participants. The `OS timer` is modeled in P using the [`Timer` machine](https://github.com/p-org/P/blob/master/Tutorial/Common/Timer/PSrc/Timer.p) declared in the [`Timer project`](https://github.com/p-org/P/tree/master/Tutorial/Common/Timer). The Timer model demonstrate how when reasoning about the correctness of a system, we need to also model its interaction to any nondeterministic environment or service (in this case, an OS timer).

- **Failure Injector:** P allows programmers to explicitly model different types of failures in the system. The [`FailureInjector`](https://github.com/p-org/P/tree/master/Tutorial/Common/FailureInjector) project demonstrate how to model node failures in P using the `halt` event. The [`FailureInjector` machine](https://github.com/p-org/P/blob/master/Tutorial/Common/FailureInjector/PSrc/FailureInjector.p) nondeterministically picks a node and sends it a `eShutDown` event. On receiving a `eShutDown` event, the corresponding node must do `halt` to destroy itself. To know more about the special `halt` event, please check the manual: [halt event](../manual/expressions.md#primitive).

### Specifications

The P Specifications ([PSpec](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSpec)) for the TwoPhaseCommit example are implemented in the [Atomicity.p](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/PSpec/Atomicity.p) file. We define two specifications:

- Atomicity (safety property): if a transaction is committed by the coordinator then it was agreed on by all participants.

- Progress (liveness property): every received transaction from a client must be eventually responded back.

!!! info "Note"
    BankBalanceIsCorrect also checks that if there is enough money in the account then the withdraw request must not error. Hence, the two properties above together ensure that every withdraw request if allowed will eventually succeed and the bank cannot block correct withdrawal requests.

??? tip "[Expand]: Lets walk through Atomicity.p"
    ...

### Test Scenarios

The test scenarios folder consists of two parts: (1) TestDrivers: These are collection of state machines that implement the test harnesses or environment state machines for different test scenarios and (2) TestScripts: These are collection of test cases that are automatically discharged by the P checker.

The test scenarios folder for TwoPhaseCommit ([PTst](https://github.com/p-org/P/tree/master/Tutorial/1_ClientServer/PTst)) consists of three files:

- [TestDriver.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PTst/TestDriver.p)
- 
- [TestScript.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PTst/Testscript.p).

??? tip "[Expand]: Lets walk through TestDriver.p"
    ...

??? tip "[Expand]: Lets walk through TestScript.p"
    ...

### Compiling TwoPhaseCommit

Run the following command to compile the TwoPhaseCommit project:

```
pc -proj:TwoPhaseCommit.pproj
```

??? note "Expected Output"
    ```
    ----------------------------------------
    ==== Loading project file: TwoPhaseCommit.pproj
    ....... includes p file:  P/Tutorial/2_TwoPhaseCommit/PSrc/Coordinator.p
    ....... includes p file:  P/Tutorial/2_TwoPhaseCommit/PSrc/Participant.p
    ....... includes p file:  P/Tutorial/2_TwoPhaseCommit/PSrc/TwoPhaseCommitModules.p
    ....... includes p file:  P/Tutorial/2_TwoPhaseCommit/PSpec/Atomicity.p
    ....... includes p file:  P/Tutorial/2_TwoPhaseCommit/PTst/TestDriver.p
    ....... includes p file:  P/Tutorial/2_TwoPhaseCommit/PTst/Client.p
    ....... includes p file:  P/Tutorial/2_TwoPhaseCommit/PTst/TestScripts.p
    ==== Loading project file:  P/Tutorial/Common/Timer/Timer.pproj
    ....... includes p file:  P/Tutorial/Common/Timer/PSrc/Timer.p
    ....... includes p file:  P/Tutorial/Common/Timer/PSrc/TimerModules.p
    ==== Loading project file:  P/Tutorial/Common/FailureInjector/FailureInjector.pproj
    ....... includes p file:  P/Tutorial/Common/FailureInjector/PSrc/NetworkFunctions.p
    ....... includes p file:  P/Tutorial/Common/FailureInjector/PSrc/FailureInjector.p
    ----------------------------------------
    ----------------------------------------
    Parsing ..
    Type checking ...
    Code generation ....
    Generated TwoPhaseCommit.cs
    ----------------------------------------
    Compiling TwoPhaseCommit.csproj ..

    Microsoft (R) Build Engine version 16.10.2+857e5a733 for .NET
    Copyright (C) Microsoft Corporation. All rights reserved.

    Determining projects to restore...
    All projects are up-to-date for restore.
    TwoPhaseCommit ->  P/Tutorial/2_TwoPhaseCommit/POutput/netcoreapp3.1/TwoPhaseCommit.dll

    Build succeeded.
        0 Warning(s)
        0 Error(s)
    ```

### Testing TwoPhaseCommit

You can get the list of test cases defined in the TwoPhaseCommit program by passing the generated `dll`
to the P Checker:

```shell
pmc <Path>/TwoPhaseCommit.dll
```

??? note "Expected Output"

    ```shell hl_lines="5 6 7"
    pmc <Path>/TwoPhaseCommit.dll

    Provide /method or -m flag to qualify the test method name you wish to use. 
    Possible options are::
    PImplementation.tcSingleClientNoFailure.Execute
    PImplementation.tcMultipleClientsNoFailure.Execute
    PImplementation.tcMultipleClientsWithFailure.Execute
    ```

There are three test cases defined in the TwoPhaseCommit project and you can specify which
test case to run by using the `-m` parameter along with the `-i` parameter for the number of schedules to explore.

Check the `tcSingleClientNoFailure` test case for 1000 schedules:

```
pmc <Path>/TwoPhaseCommit.dll \
    -m PImplementation.tcSingleClientNoFailure.Execute \
    -i 1000
```

Check the `tcMultipleClientsNoFailure` test case for 1000 schedules:

```
pmc <Path>/TwoPhaseCommit.dll \
    -m PImplementation.tcMultipleClientsNoFailure.Execute \
    -i 1000
```

Check the `tcMultipleClientsWithFailure` test case for 1000 schedules:

```
pmc <Path>/TwoPhaseCommit.dll \
    -m PImplementation.tcMultipleClientsWithFailure.Execute \
    -i 1000
```

!!! danger "Error"
    `tcMultipleClientsWithFailure` triggers an error in the AbstractBankServer state machine. Please use the [guide](../advanced/debuggingerror.md) to explore how to debug an error trace generated by P Checker.



### Exercise Problem

!!! summary "What did we learn through this example?"
    We dived deeper into: (1) modeling non-determinism in distributed systems, in particular, time-outs (2) writing complex safety properties like atomicity of transactions in P and finally, (3) modeling node failures in P using a failure injector state machine. We will also show how P allows invoking foreign code from the P programs. More details in [P foreign interface](../manual/foriegntypesfunctions.md).

