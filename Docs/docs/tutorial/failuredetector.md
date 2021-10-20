
Energized with the Coffee :coffee:, lets get back to modeling distributed systems. After the two phase commit protocol, the next protocol that we will jump to is a simple broadcast-based failure detector!

By this point in the tutorial, we have gotten familiar with the P language and most of its features. So, working through this example should be super fast! 

??? note "How to use this example"

    We assume that you have cloned the P repository locally.
    ```shell 
    git clone https://github.com/p-org/P.git
    ```

    The recommended way to work through this example is to open the [P\Tutorial](https://github.com/p-org/P/tree/master/Tutorial) folder in IntelliJ side-by-side a browser using which you can simulatenously read the description for each example and browser the P program in IntelliJ. 

**System:** We consider a simple failure detector that basically broadcasts ping messages to all the nodes in the system and uses a timer to wait for pong responses from all nodes. If a node does not respond with a pong message after multiple attempts (either because of network failure or node failure), the failure detector marks the node as down and notifies the clients about the nodes that are potentially down. We use this example to show how to model network message loss in P and discuss how to model other types of network behaviours.

![Placeholder](failuredetector.png){ align=center }

**Correctness Specification:** We would like to check using a liveness specification that if the failure injecter shutsdown a particular node then the failure detector always eventually detects that the node failure and notifies the client.

### P Project

The [4_FailureDetector](https://github.com/p-org/P/tree/master/Tutorial/4_FailureDetector) folder contains the source code for the [FailureDetector](https://github.com/p-org/P/blob/master/Tutorial/4_FailureDetector/FailureDetector.pproj) project. Please feel free to read details about the recommended [P program structure](../advanced/structureOfPProgram.md) and [P project file](../advanced/PProject.md).

### Models

The P models ([PSrc](https://github.com/p-org/P/tree/master/Tutorial/4_FailureDetector/PSrc)) for the FailureDetector example consists of four files:

- [FailureDetector.p](https://github.com/p-org/P/blob/master/Tutorial/4_FailureDetector/PSrc/FailureDetector.p): Implements the `FailureDetector` state machine.
  
??? tip "[Expand]: Lets walk through FailureDetector.p"

    - ([L1 - L4](https://github.com/p-org/P/blob/master/Tutorial/4_FailureDetector/PSrc/FailureDetector.p#L1-L4))  &rarr; Event `ePing` and `ePong` are used to communicate between the `FailureDetector` and the `Node` state machines (manual: [event declaration](../manual/events.md)).
    - ([L6](https://github.com/p-org/P/blob/master/Tutorial/4_FailureDetector/PSrc/FailureDetector.p#L6)) &rarr; Event `eNotifyNodesDown` is used by the FailureDetector to inform the clients about the nodes that are potentially down.
    - ([L14 - L129](https://github.com/p-org/P/blob/master/Tutorial/4_FailureDetector/PSrc/FailureDetector.p#L14-L129))  &rarr; Declares the `FailureDetector` state machine (manual: [P state machine](../manual/statemachines.md)). The key points to note in the `FailureDetector` machine is the usage of [Timer](https://github.com/p-org/P/tree/master/Tutorial/Common/Timer) machine to model the usage of OS timer, the usage of [ReliableBroadCast](https://github.com/p-org/P/blob/master/Tutorial/4_FailureDetector/PSrc/FailureDetector.p#L81) and [UnReliableBroadCast](https://github.com/p-org/P/blob/master/Tutorial/4_FailureDetector/PSrc/FailureDetector.p#L48) defined in [NetworkFunctions.p](https://github.com/p-org/P/blob/master/Tutorial/Common/FailureInjector/PSrc/NetworkFunctions.p).

- [Node.p](https://github.com/p-org/P/blob/master/Tutorial/4_FailureDetector/PSrc/Node.p): Implements the `Node` state machine.
  
??? tip "[Expand]: Lets walk through Node.p"
    - ([L4 - L14](https://github.com/p-org/P/blob/master/Tutorial/4_FailureDetector/PSrc/Node.p#L4-L14)) &rarr; Declares the `Node` state machine. The `Node` machine responds with a `ePong` message on receiving a `ePing` message from the `FailureDetector`. On receiving a `eShutDown` message the `FailureInjector`, the machine halts itself.

- [FailureDetectorModules.p](https://github.com/p-org/P/blob/master/Tutorial/4_FailureDetector/PSrc/FailureDetectorModules.p): Declares the `FailureDetector` module.

??? tip "[Expand]: Lets walk through FailureDetectorModules.p"
    Declares the `FailureDetector` module which is the union of module consisting of the `FailureDetector`, `Node`, `Client` machines and the `Timer` module.

!!! info "Key Takeaway"
    To mitigate the state space explosion problem, when modeling and checking complex systems consisting of several components, we would like to check the correctness of each component in isolation. When doing this kind of a compositional reasoning, we would like to replace the environment of the component with its abstraction. The abstraction basically exposes the same interface as the environment by removes its internal complexity, simplifying the overall problem of checking the correctness of the component under test. There is a large body of literature on doing compositional reasoning of distributed systems. You can start with [the Modular P paper](https://ankushdesai.github.io/assets/papers/modp.pdf). How to automatically replace a machine with its abstraction is described below.

- [ FailureDetectorModules.p](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PSrc/ FailureDetectorModules.p): Declares the P modules corresponding to each component in the system.

??? tip "[Expand]: Lets walk through  FailureDetectorModules.p"
    - ([L1 - L5](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PSrc/ FailureDetectorModules.p#L1-L5)) &rarr; Declares the `Client` and `Bank` modules. A module in P is a collection of state machines that together implement that module or component. A system model in P is then a composition or union of modules. The `Client` module consist of a single machine `Client` and the `Bank` module is implemented by machines `BankServer` and `Database` together (manual: [P module system](../manual/modulesystem.md)).  The `AbstractBank` module using the `binding` feature in P modules to bind the `BankServer` machine to `AbstractBankServer` machine. Basically, what this implies is that whenever `AbstractBank` module is used creation of the `BankServer` machine will result in creation of `AbstractBankServer`, replacing the implementation with its abstraction (manual: [primitive modules](../manual/modulesystem.md#primitive-module)).

### Specifications

The P Specifications ([PSpec](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PSpec)) for the  FailureDetector example are implemented in the [BankBalanceCorrect.p](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PSpec/BankBalanceCorrect.p) file. We define two specifications:

- **BankBalanceIsAlwaysCorrect** (safety property): BankBalanceIsCorrect spec checks the global invariant that the account-balance communicated to the client by the bank is always correct and the bank never removes more money from the account than what is withdrawn by the client! Also, if the bank denies a withdraw request then its only because the withdrawal will reduce the account balance to below 10.

- **GuaranteedWithDrawProgress** (liveness property): GuaranteedWithDrawProgress checks the liveness (or progress) property that all withdraw requests submitted by the client are eventually responded.

!!! info ""
    BankBalanceIsAlwaysCorrect also checks that if there is enough money in the account then the withdraw request must not error. Hence, the two properties above together ensure that every withdraw request if allowed will eventually succeed and the bank cannot block correct withdrawal requests.

??? tip "[Expand]: Lets walk through BankBalanceCorrect.p"
    - ([L20](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PSpec/BankBalanceCorrect.p#L20)) &rarr; Event `eSpec_BankBalanceIsAlwaysCorrect_Init` is used to inform the monitors about the initial state of the Bank. The event is announced by the TestDrivers when setting up the system ([here](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PTst/TestDriver.p#L51)).
    - ([L36 - L86](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PSpec/BankBalanceCorrect.p#L36-L86)) &rarr; Declares the `BankBalanceIsAlwaysCorrect` safety spec machine that observes the events `eWithDrawReq`,  `eWithDrawResp`, and `eSpec_BankBalanceIsAlwaysCorrect_Init` to assert the required global invariant.
    - ([L92 - L115](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PSpec/BankBalanceCorrect.p#L92-L115)) &rarr; Declares the `GuaranteedWithDrawProgress` liveness spec machine that observes the events `eWithDrawReq` and `eWithDrawResp` to assert the required liveness property that every request is eventually responded by the Bank.
    - To understand the semantics of the P spec machines, please read manual: [p monitors](../manual/monitors.md).

### Test Scenarios

The test scenarios folder in P has two parts: (1) TestDrivers: These are collection of state machines that implement the test harnesses or environment state machines for different test scenarios and (2) TestScripts: These are collection of test cases that are automatically discharged by the P checker.

The test scenarios folder for  FailureDetector ([PTst](https://github.com/p-org/P/tree/master/Tutorial/1_ FailureDetector/PTst)) consists of two files [TestDriver.p](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PTst/TestDriver.p) and [TestScript.p](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PTst/Testscript.p).

??? tip "[Expand]: Lets walk through TestDriver.p"
    - ([L36 - L60](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PTst/TestDriver.p#L36-L60)) &rarr; Function `Setup FailureDetectorSystem` takes as input the number of clients to be created and setups the  FailureDetector system by creating the `Client` and `BankServer` machines. The [`CreateRandomInitialAccounts`](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PTst/TestDriver.p#L25-L34) function uses the [`choose`](../manual/expressions.md#choose) primitive to randomly initialize the accounts map.
    - ([L3 - L22](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PTst/TestDriver.p#L3-L22)) &rarr; Machines `TestWithSingleClient` and `TestWithMultipleClients` are simple test driver machines that setup the system to be checked by the P checker for different scenarios. In this case, test the  FailureDetector system by first randomly initializing the accounts map and with one `Client` and with multiple `Client`s (between 2 to 4)).

??? tip "[Expand]: Lets walk through TestScript.p"
    P allows programmers to write different test cases each of which can be checked separately and each can use a different test driver that triggers different behaviors in the system under test using different system configurations and input generators.

    - To better understand the P test cases, please look at manual: [P test cases](../manual/testcases.md).
    - ([L4 - L16](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PTst/Testscript.p#L4-L16)) &rarr; Declares three test cases each checking a different scenario and system. The system under test is the `union` of the modules representing each component in the system (manual: [P module system](../manual/modulesystem.md#union-module)).
    - In the test case `tcSingleClientAbstractServer`, instead of composing with the Bank module, [we use the AbstractBank module](https://github.com/p-org/P/blob/master/Tutorial/1_ FailureDetector/PTst/Testscript.p#L16). Hence, in the composed system, whenever we create `BankServer` machine during the execution of the system it leads to the creation of the `AbstractBankServer` machine.

### Compiling FailureDetector

Run the following command to compile the FailureDetector project:

```
pc -proj:FailureDetector.pproj
```

??? note "Expected Output"
    ```
    ----------------------------------------
    ==== Loading project file: FailureDetector.pproj
    ....... includes p file: P/Tutorial/4_FailureDetector/PSrc/FailureDetectorModules.p
    ....... includes p file: P/Tutorial/4_FailureDetector/PSrc/FailureDetector.p
    ....... includes p file: P/Tutorial/4_FailureDetector/PSrc/Node.p
    ....... includes p file: P/Tutorial/4_FailureDetector/PSpec/ReliableFailureDetector.p
    ....... includes p file: P/Tutorial/4_FailureDetector/PTst/TestDriver.p
    ....... includes p file: P/Tutorial/4_FailureDetector/PTst/TestScript.p
    ....... includes p file: P/Tutorial/4_FailureDetector/PTst/Client.p
    ==== Loading project file: P/Tutorial/Common/Timer/Timer.pproj
    ....... includes p file: P/Tutorial/Common/Timer/PSrc/Timer.p
    ....... includes p file: P/Tutorial/Common/Timer/PSrc/TimerModules.p
    ==== Loading project file: P/Tutorial/Common/FailureInjector/FailureInjector.pproj
    ....... includes p file: P/Tutorial/Common/FailureInjector/PSrc/NetworkFunctions.p
    ....... includes p file: P/Tutorial/Common/FailureInjector/PSrc/FailureInjector.p
    ----------------------------------------
    ----------------------------------------
    Parsing ..
    Type checking ...
    Code generation ....
    Generated FailureDetector.cs
    ----------------------------------------
    Compiling FailureDetector.csproj ..

    Microsoft (R) Build Engine version 16.10.2+857e5a733 for .NET
    Copyright (C) Microsoft Corporation. All rights reserved.

    Determining projects to restore...
    Restored P/Tutorial/4_FailureDetector/FailureDetector.csproj (in 880 ms).
    FailureDetector -> P/Tutorial/4_FailureDetector/POutput/netcoreapp3.1/FailureDetector.dll

    Build succeeded.
        0 Warning(s)
        0 Error(s)

    ```

### Testing FailureDetector

There is only a single test case in the FailureDetector program and we can directly run the test case for 10000 iterations:

```shell
pmc <Path>/FailureDetector.dll -i 10000
```

### Discussion: Modeling Message Reordering

### Exercise Problem

!!! success "What did we learn through this example?"
    In this example, we saw how to use data nondeterminism to model message loss and unreliable sends. We also discussed how to model other types of network nondeterminism.

