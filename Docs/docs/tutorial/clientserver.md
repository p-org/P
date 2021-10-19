??? note "How to use this example"

    We assume that you have cloned the P repository locally.
    ```shell 
    git clone https://github.com/p-org/P.git
    ```

    The recommended way to work through this example is to open the [P\Tutorial](https://github.com/p-org/P/tree/master/Tutorial) folder in IntelliJ side-by-side a browser using which you can simulatenously read the description for each example and browser the P program. 

**System:** We consider a client-server application where clients interact with the bank to withdraw money from their accounts. 

![Placeholder](clientserver.png){ align=center }

The bank consists of two components: a bank server that services withdraw requests from the client and a backend database which is used to store the account balance information for each client.
Multiple clients can concurrently send withdraw request to the bank server. The bank server on receiving a withdraw request, reads the current bank balance for the client and if the withdraw request is allowed then performs the withdrawal, updates the account balance and responds back to the client with the new account balance.

**Correctness Specification:** One of the invariant that the bank tries to maintain is that each client account must have atleast 10 dollars as its balance. If a withdraw request can take the account balance below 10 then the withdraw request is rejected by the bank. We would like to check the correctness property that in the presence of concurrent client withdraw requests the bank always responds with the correct bank balance for a client and a withdraw request must always succeeds if there is enough balance (> 10) in the account.



### P Project

The [1_ClientServer](https://github.com/p-org/P/tree/master/Tutorial/1_ClientServer) folder contains the source code for the [ClientServer](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/ClientServer.pproj) project. Please feel free to read details about the typical [P program structure](../advanced/structureOfPProgram.md) and [P project file](../advanced/PProject.md).

### Models

The P models ([PSrc](https://github.com/p-org/P/tree/master/Tutorial/1_ClientServer/PSrc)) for the ClientServer example consists of four files: 

1. [Client.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSrc/Client.p): Implements the Client state machine.
  
??? tip "[Expand]: Lets walk through Client.p"
    - ([L19-L22](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSrc/Client.p#L19-L22)) :: Event `eWithDrawReq` and `eWithDrawResp` are used to communicate between the `Client` and `Server` machine ([Event Declarations](../manual/events.md)).
    - ([L3-L17](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSrc/Client.p#L3-L17)) :: Declares the payload types for the `eWithDrawReq` and `eWithDrawResp` events ([User Defined Type Declarations](../manual/datatypes.md#user-defined))
    - ([L25-L95](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSrc/Client.p#L25-L95)) :: Declares the `Client` state machine ([P State Machine Declaration](../manual/statemachines.md)).



- [Server.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSrc/Server.p): Implements the BankServer and the Backend Database state machines.
  
??? tip "[Expand]: Lets walk through Server.p"
    ...

- [AbstractBankServer.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSrc/AbstractBankServer.p): Implements the AbstractBankServer state machine that provides an simplified abstraction for the BankServer machine.

We will demonstrate how one replace the bank service consisting of two interacting components by its abstraction when testing the client application.

??? tip "[Expand]: Lets walk through AbstractBankServer.p"
    ...

- [ClientServerModules.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSrc/ClientServerModules.p): Declares the P modules corresponding to each component in the system.

??? tip "[Expand]: Lets walk through ClientServerModules.p"
    ...

### Specifications

The P Specifications ([PSpec](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSpec)) for the ClientServer example are implemented in the [BankBalanceCorrect.p](https://github.com/p-org/P/blob/master/Tutorial/1_ClientServer/PSpec/BankBalanceCorrect.p) file. We define two specifications:

- BankBalanceIsAlwaysCorrect (safety property): BankBalanceIsCorrect spec checks the global invariant that the account-balance communicated to the client by the bank is always correct and the bank never removes more money from the account than what is withdrawn by the client! Also, if the bank denies a withdraw request then its only because the withdrawal will reduce the account balance to below 10.

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

- [Problem 1] Fix the bug in AbstractBankServer state machine and run the P Checker again on the test cases to ensure that there are no more bugs in the models.
- [Problem 2] Extend the ClientServer example with support for depositing money into the bank. This would require implementing events `eDepositReq` and `eDepositResp` which are used to interact between the client and server machine. The Client machine should be updated to deposit (one time) some random money when the account balance is low and the BankServer machine implementation would have to be updated to support depositing money into the account. After implementing the deposit feature, run the test-cases again to check if the system still satisfies the desired specifications. 

!!! success "What did we learn through this example?"
    We will learn about P state machines, writing simple safety and liveness specifications as P monitors, writing multiple model checking scenarios to check the correctness of a P program, and finally, replacing complex components in P with their abstractions using P's module system.

