??? note "How to use this example"

    We assume that you have cloned the P repository locally.
    ```shell 
    git clone https://github.com/p-org/P.git
    ```

    The recommended way to work through this example is to open the [P\Tutorial](https://github.com/p-org/P/tree/master/Tutorial) folder in IntelliJ side-by-side a browser using which you can simulatenously read the description for each example and browser the P program in IntelliJ.

    To know more about P language primitives used in the example, please look them up in the [language manual](../manualoutline.md).

Now that we understand the basic features of the P language, let's look at modeling and analysis of a distributed system :man_juggling:!


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
  
??? tip "[Expand]: Let's walk through Coordinator.p"
    - ([L25 - L33](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/PSrc/Coordinator.p#L25-L33)) &rarr; Declares the `write` and `read` transaction events used to communicate between the coordinator and the client machines (manual: [event declaration](../manual/events.md)).
    - ([L35 - L43](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/PSrc/Coordinator.p#L35-L43)) &rarr; Declares the `prepare`, `commit` and `abort` events used to communicate between the coordinator and the participants in the system.
    - ([L3 - L16](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/PSrc/Coordinator.p#L3-L16)) &rarr; Declares the payload types associated with these events (manual: [user defined type](../manual/datatypes.md#user-defined)).
    - ([L65 - L177](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/PSrc/Coordinator.p#L65-L177)) &rarr; Declares the `Coordinator` state machine. The Coordinator machine receives write and read transactions from the clients. The coordinator machine
    services these transactions one by one in the order in which they were received. On receiving a write
    transaction the coordinator sends prepare request to all the participants and waits for prepare
    responses from all the participants. Based on the responses, the coordinator either commits or aborts
    the transaction. If the coordinator fails to receive agreement from participants in time, then it
    timesout and aborts the transaction. On receiving a read transaction, the coordinator randomly selects
    a participant and  forwards the read request to that participant.
- [Participant.p](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/PSrc/Participant.p): Implements the Participant state machine.
  
??? tip "[Expand]: Let's walk through Participant.p"
    - Unlike the `Coordinator` state machine that has multiple states, the `Participant` state machine is fairly simple. Each participant waits for requests from the `Coordinator` and sends the response back based on whether the request can be accepted or has to be rejected.
    - On receiving a `eShutDown` event, the participant does a `raise halt` to destroy itself. To know more about the special `halt` event, please check the manual: [halt event](../manual/expressions.md#primitive).
    - Each participant maintains a local key-value store which is updated based on the transactions committed by the coordinator. On receiving a prepare request from the coordinator, the participant chooses to either accept or reject the transaction based on the associated transaction id.
  
- [TwoPhaseCommitModules.p](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/PSrc/TwoPhaseCommitModules.p): Declares the P module corresponding to the two phase commit system.

### Timer and Failure Injector

Our two phase commit project dependends on two other components:

- **OS Timer:** The coordinator machine uses a timer to wait for prepare responses from all participants. The `OS timer` is modeled in P using the [`Timer` machine](https://github.com/p-org/P/blob/master/Tutorial/Common/Timer/PSrc/Timer.p) declared in the [`Timer project`](https://github.com/p-org/P/tree/master/Tutorial/Common/Timer). The Timer model demonstrate how when reasoning about the correctness of a system, we need to also model its interaction to any nondeterministic environment or service (in this case, an OS timer).

- **Failure Injector:** P allows programmers to explicitly model different types of failures in the system. The [`FailureInjector`](https://github.com/p-org/P/tree/master/Tutorial/Common/FailureInjector) project demonstrate how to model node failures in P using the `halt` event. The [`FailureInjector` machine](https://github.com/p-org/P/blob/master/Tutorial/Common/FailureInjector/PSrc/FailureInjector.p) nondeterministically picks a node and sends it a `eShutDown` event. On receiving a `eShutDown` event, the corresponding node must do `halt` to destroy itself. To know more about the special `halt` event, please check the manual: [halt event](../manual/expressions.md#primitive).

### Specifications

The P Specifications ([PSpec](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSpec)) for the TwoPhaseCommit example are implemented in the [Atomicity.p](https://github.com/p-org/P/blob/master/Tutorial/2_TwoPhaseCommit/PSpec/Atomicity.p) file. We define two specifications:

- **Atomicity** (safety property): if a transaction is committed by the coordinator then it was agreed on by all participants and if the transaction is aborted then atleast one participant must have rejected the transaction.

- **Progress** (liveness property): every received transaction must be eventually responded back (in the absence of node failures).

!!! info "Weaker Property"
    Note that we are asserted a weaker property than what is required for Atomicity. Ideally, we would like to check that if a transaction is committed by the coordinator then it was committed-locally by all participants and if the transaction is aborted then atleast one participant must have rejected the transaction and all the participants aborted the transaction. But we leave implementing this stronger property as an exercise problem which you can revisit after finishing the other problems in the tutorials.

### Test Scenarios

The test scenarios folder in P has two parts: TestDrivers and TestScripts. TestDrivers are collections of state machines that implement the test harnesses (or environment state machines) for different test scenarios. TestScripts are collections of test cases that are automatically run by the P checker.

The test scenarios folder for ClientServer ([PTst](https://github.com/p-org/P/tree/master/Tutorial/1_ClientServer/PTst)) consists of two files [TestDriver.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PTst/TestDriver.p) and [TestScript.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PTst/Testscript.p).

??? tip "[Expand]: Let's walk through TestDriver.p"
    - ([L36 - L60](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PTst/TestDriver.p#L36-L60)) &rarr; Function `SetupClientServerSystem` takes as input the number of clients to be created and configures the ClientServer system by creating the `Client` and `BankServer` machines. The [`CreateRandomInitialAccounts`](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PTst/TestDriver.p#L25-L34) function uses the [`choose`](../manual/expressions.md#choose) primitive to randomly initialize the accounts map.
    - ([L3 - L22](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PTst/TestDriver.p#L3-L22)) &rarr; Machines `TestWithSingleClient` and `TestWithMultipleClients` are simple test driver machines that configure the system to be checked by the P checker for different scenarios. In this case, test the ClientServer system by first randomly initializing the accounts map and then testing it with either one `Client` or with multiple `Client`s (between 2 and 4)).

??? tip "[Expand]: Let's walk through TestScript.p"
    P allows programmers to write different test cases. Each test case is checked separately and can use a different test driver. Using different test drivers triggers different behaviors in the system under test, as it implies different system configurations and input generators. To better understand the P test cases, please look at manual: [P test cases](../manual/testcases.md).
    - ([L4 - L16](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PTst/Testscript.p#L4-L16)) &rarr; Declares three test cases each checking a different scenario and system. The system under test is the `union` of the modules representing each component in the system (manual: [P module system](../manual/modulesystem.md#union-module)).
    - In the `tcSingleClientAbstractServer` test case, instead of composing with the Bank module, we use the AbstractBank module. Hence, in the composed system, whenever the creation of a BankServer machine is invoked the binding will instead create an AbstractBankServer machine.
  
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

Check the `tcSingleClientNoFailure` test case for 10000 schedules:

```
pmc <Path>/TwoPhaseCommit.dll \
    -m PImplementation.tcSingleClientNoFailure.Execute \
    -i 10000
```

Check the `tcMultipleClientsNoFailure` test case for 10000 schedules:

```
pmc <Path>/TwoPhaseCommit.dll \
    -m PImplementation.tcMultipleClientsNoFailure.Execute \
    -i 10000
```
!!! danger "Error"
    `tcMultipleClientsNoFailure` triggers a very subtle concurrency bug in the assumption made by the client of the two phase commit protocol.

    `<ErrorLog> Assertion Failed: Record read is not same as what was written by the client:: read - X, written - Y`

    Please use the [guide](../advanced/debuggingerror.md) to explore how to debug an error trace generated by P Checker.

!!! hint "Hint"

    If you dive deeper in the log, you would notice that the error happens when two clients write to the same key and there is a race between the two writes and reads. The fix for this problem is to update the assertion on the client side that if the value that is read is not the same as the value that was written then it has to be true that it was overridden by a transaction whose transaction id is greater than its transaction id. This is true because Participants accept transaction to the same key in the monotonically increasing transaction ids.

Check the `tcMultipleClientsWithFailure` test case for 10000 schedules:

```
pmc <Path>/TwoPhaseCommit.dll \
    -m PImplementation.tcMultipleClientsWithFailure.Execute \
    -i 10000
```

!!! danger "Error"
    `tcMultipleClientsWithFailure` demonstrates the known case that in the presence of node failure the two phase commit protocol does not guarantee progress. 

    `<ErrorLog> Deadlock detected. Client(7) is waiting to receive an event, but no other controlled tasks are enabled.`

    Please use the [guide](../advanced/debuggingerror.md) to explore how to debug an error trace generated by P Checker.

    If you comment out the part of the Client state machine code that performs the read transaction then you would see the the Progress liveness monitor starts complaining.



### Exercise Problem

- [Problem 1] Based on the hint above, try and fix the concurrency bug in the `Client` state machine and run the test cases again!

- [Problem 2] A really interesting exploratory problem would be to try and combine the two phase commit protocol with the failure detector system to over come the progress problem faced by two phase commit protocol in the presence of node failures. Can you really do that? Let's have a discussion and build a variant of the protocol to tolerant failures?

!!! summary "What did we learn through this example?"
    We dived deeper into: (1) modeling non-determinism in distributed systems, in particular, time-outs (2) writing complex safety properties like atomicity of transactions in P and finally, (3) modeling node failures in P using a failure injector state machine. We will also show how P allows invoking foreign code from the P programs. More details in [P foreign interface](../manual/foriegntypesfunctions.md).

