
Energized with the Coffee :coffee:, lets get back to modeling distributed systems. After the two phase commit protocol, the next protocol that we will jump to is a simple broadcast-based failure detector!

By this point in the tutorial, we have gotten familiar with the P language and most of its features. So, working through this example should be super fast! 

??? note "How to use this example"

    We assume that you have cloned the P repository locally.
    ```shell 
    git clone https://github.com/p-org/P.git
    ```

    The recommended way to work through this example is to open the [P\Tutorial](https://github.com/p-org/P/tree/master/Tutorial) folder in IntelliJ side-by-side a browser using which you can simulatenously read the description for each example and browser the P program in IntelliJ. 

**System:** We consider a simple broadcast based failure detector that basically broadcasts ping messages to all nodes in the system and uses a timer to wait for a pong response from all nodes. If a certain node does not respond with a pong message after multiple attempts (either because of network failure or node failure), the failure detector marks the node as down and notifies the clients about the nodes that are potentially down. We use this example to show how to model network message loss in P and discuss how to model other types of network behaviours.

![Placeholder](failuredetector.png){ align=center }

**Correctness Specification:** We would like to check using a liveness specification that if the failure injecter shutsdown a particular node then the failure detector always eventually detects that node has failed and notifies client.

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

### Compiling ClientServer

### Testing ClientServer

### Exercise Problem

!!! summary "Summary"
    In this example, we demonstrate how to use data nondeterminism to model message loss and unreliable sends. We also discuss how to model other types of network nondeterminism. Finally, we give another example of a liveness specification that the failure detector must satisfy.


