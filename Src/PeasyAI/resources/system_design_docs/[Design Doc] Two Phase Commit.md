# Two Phase Commit Protocol

## Introduction

The goal of this system is to model a simplified version of the classic two phase commit protocol. The two phase commit protocol uses a coordinator to gain consensus for any transaction spanning across multiple participants. A transaction in our case is simply a write operation for a key-value data store where the data store is replicated across multiple participants. More concretely, a write transaction must be committed by the coordinator only if it's accepted by all the participant replicas, and must be aborted if any one of the participant replicas rejects the write request.

**Assumptions:**
1. Our system allows multiple concurrent clients to issue transactions in parallel, but the coordinator serializes these transactions and services them one-by-one.
2. Our system is not fault-tolerant to node failures, failure of either the coordinator or any of the participants will block the progress forever.
3. Our system models assume reliable delivery of messages.
4. The Timer machine is a pre-existing reusable module — do NOT re-implement it. Use CreateTimer(this), StartTimer(timer), and CancelTimer(timer) to interact with the timer.

## Components

### Source Components

#### 1. Coordinator
- **Role:** Receives write and read transactions from the clients and orchestrates the two-phase commit protocol.
- **States:** WaitForRequests, WaitForPrepareResponses
- **Local state:**
    - `participants`: the list of participant machines
    - `timer`: timer for prepare-phase timeout
    - `pendingTransaction`: the currently active transaction
    - `prepareResponses`: responses from participants
    - `currentClient`: the client that issued the current transaction
- **Initialization:** Created with references to all participant machines. On startup, creates a timer and sends its own reference to all participants so they know who the coordinator is.
    - On initialization: creates a timer using CreateTimer(this), then sends eInformCoordinator to all participants with its own reference so participants know who the coordinator is.
- **Behavior:**
    - Processes transactions (write and read) from clients one by one in the order received.
    - On receiving a write transaction: sends prepare requests to all participants, starts the timer, and waits for prepare responses. Commits if all participants accepted, aborts if any rejected. Times out and aborts if responses are not received in time.
    - On receiving a read transaction: randomly selects a participant using choose() and forwards the read request.
- **Event handling notes:**
    - In WaitForRequests: ignore stale `ePrepareResp` and `eTimeOut`
    - In WaitForPrepareResponses: defer `eWriteTransReq` and `eReadTransReq`

#### 2. Participant
- **Role:** Maintains a local key-value store replica and votes on transactions.
- **States:** WaitForCoordinator, Ready
- **Local state:**
    - `coordinator`: reference to the coordinator
    - `kvStore`: local key-value store
    - `pendingTransactions`: pending prepare requests
- **Initialization:** Waits for the coordinator to send its reference before becoming operational.
- **Behavior:**
    - Waits for eInformCoordinator from the Coordinator (blocks in Init state until informed).
    - On ePrepareReq: non-deterministically accepts (using choose()) or rejects the transaction. Sends ePrepareResp back to coordinator.
    - On eCommitTrans: commits the transaction to the local store.
    - On eAbortTrans: removes the transaction from pending.

### Test Components

#### 3. Client
- **Role:** Issues transactions to the coordinator and validates results.
- **Local state:**
    - `coordinator`: reference to the coordinator
    - `numTransactions`: number of transactions to issue
- **Initialization:** Created with a reference to the coordinator and the number of transactions to issue.
- **Behavior:**
    - Issues N non-deterministic write-transactions (with random key and value using choose()).
    - On success response: performs a read-transaction on the same key and asserts the value read matches what was written.
    - On failure response: moves on to the next transaction.

## Interactions

1. **eWriteTransReq**
    - **Source:** Client
    - **Target:** Coordinator
    - **Payload:** the client's reference and the transaction (key, value, transaction ID)
    - **Description:** Client sends a write transaction request to the coordinator.

2. **eWriteTransResp**
    - **Source:** Coordinator
    - **Target:** Client
    - **Payload:** the transaction ID and the status (SUCCESS or ERROR)
    - **Description:** Coordinator sends the result of the write transaction request back to the client.

3. **eReadTransReq**
    - **Source:** Client
    - **Target:** Coordinator
    - **Payload:** the client's reference and the key to read
    - **Description:** Client requests to read a specific key.

4. **eReadTransResp**
    - **Source:** Participant
    - **Target:** Client
    - **Payload:** the key, the value read, and the status (SUCCESS or ERROR)
    - **Description:** Participant responds with the value read and status.

5. **ePrepareReq**
    - **Source:** Coordinator
    - **Target:** Participant
    - **Payload:** the coordinator's reference, the transaction ID, the key, and the value
    - **Description:** Coordinator requests the participant to prepare for a transaction.

6. **ePrepareResp**
    - **Source:** Participant
    - **Target:** Coordinator
    - **Payload:** the participant's reference, the transaction ID, and the status (SUCCESS or ERROR)
    - **Description:** Participant responds to the prepare request with SUCCESS or ERROR.

7. **eCommitTrans**
    - **Source:** Coordinator
    - **Target:** Participant
    - **Payload:** the transaction ID to commit
    - **Description:** Coordinator requests the participant to commit a transaction.

8. **eAbortTrans**
    - **Source:** Coordinator
    - **Target:** Participant
    - **Payload:** the transaction ID to abort
    - **Description:** Coordinator requests the participant to abort a transaction.

9. **eInformCoordinator**
    - **Source:** Coordinator
    - **Target:** Participant
    - **Payload:** the coordinator's machine reference
    - **Description:** Coordinator sends its own reference to the participant so the participant knows who to respond to. Sent during initialization after the coordinator is created.

## Specifications

1. **Atomicity** (safety property):
   If the coordinator sends an eWriteTransResp with SUCCESS to the client, then every ePrepareResp received for that transaction must have been SUCCESS, and an eCommitTrans must have been sent to all participants. If the coordinator sends an eWriteTransResp with ERROR, an eAbortTrans must have been sent — meaning at least one participant rejected or the timer expired.

## Test Scenarios

1. 3 participants, 1 coordinator, 1 client, no failure — test basic commit and abort paths.
2. 3 participants, 1 coordinator, 2 clients — test concurrent transaction serialization.
