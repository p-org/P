# Failure Detector

## Introduction

The goal of this system is to model an eventually perfect failure detector using heartbeat-based monitoring. Nodes periodically send heartbeats to a failure detector, which monitors node liveness and reports suspected failures to clients.

**Assumptions:**
1. Nodes can fail by stopping (crash failures).
2. The failure detector uses periodic heartbeat timeouts to detect failures.
3. The failure detector may temporarily suspect a live node (unreliable detection) but will eventually be accurate for permanently failed nodes.
4. Communication is reliable between live nodes.
5. The Timer machine is a pre-existing reusable module — do NOT re-implement it. Use CreateTimer(this), StartTimer(timer), and CancelTimer(timer).

## Components

### Source Components

#### 1. FailureDetector
- **Role:** Monitors a set of nodes by expecting periodic heartbeats and reports suspected failures.
- **States:** Init, Monitoring
- **Local state:**
    - `nodes`: the set of nodes being monitored
    - `aliveNodes`: nodes that have sent heartbeats since last check
    - `suspectedNodes`: nodes currently suspected of failure
    - `clients`: registered clients to notify
    - `timer`: timer for periodic liveness checks
- **Initialization:** Created with references to all the nodes it will monitor.
- **Behavior:**
    - Uses a timer to trigger periodic liveness checks.
    - If a suspected node later sends a heartbeat, it is restored to alive status.
    - Reports suspected failures to registered clients.

#### 2. Node
- **Role:** Periodically sends heartbeats to the failure detector.
- **States:** Init, Alive, Crashed
- **Local state:**
    - `failureDetector`: reference to the failure detector
    - `timer`: timer for heartbeat interval
- **Initialization:** Created with a reference to the failure detector.
- **Behavior:**
    - Periodically sends heartbeats to the failure detector.
    - Can be instructed to crash (stop sending heartbeats) to simulate failure.

### Test Components

#### 3. Client
- **Role:** Registers with the failure detector to receive failure notifications.
- **Local state:**
    - `failureDetector`: reference to the failure detector
    - `suspectedNodes`: nodes reported as suspected
- **Initialization:** Created with a reference to the failure detector.
- **Behavior:**
    - Registers with the failure detector to receive failure notifications.
    - Receives notifications when a node is suspected of failure.

## Interactions

1. **eHeartbeat**
    - **Source:** Node
    - **Target:** FailureDetector
    - **Payload:** the source node's reference
    - **Description:** Node sends a heartbeat to indicate it is alive.
    - **Effects:**
        - FailureDetector marks the node as alive and removes it from suspected set.

2. **eTimeOut**
    - **Source:** Timer
    - **Target:** FailureDetector
    - **Payload:** none
    - **Description:** Timer fires to trigger a liveness check round.
    - **Effects:**
        - FailureDetector checks which nodes have not sent heartbeats since last check.
        - Nodes that missed heartbeats are added to the suspected set.
        - Suspected nodes are reported to registered clients.

3. **eNodeSuspected**
    - **Source:** FailureDetector
    - **Target:** Client
    - **Payload:** the suspected node's reference
    - **Description:** FailureDetector notifies the client that a node is suspected of failure.

4. **eCrash**
    - **Source:** Test driver
    - **Target:** Node
    - **Payload:** none
    - **Description:** Instructs a node to stop sending heartbeats (simulating a crash).

5. **eRegisterClient**
    - **Source:** Client
    - **Target:** FailureDetector
    - **Payload:** the client's reference
    - **Description:** Client registers to receive failure notifications.

## Specifications

1. **ReliableDetection** (safety property):
   If a node receives an eCrash and stops sending eHeartbeat messages, the failure detector must eventually issue an eNodeSuspected for that node and never remove it from the suspected set thereafter.

## Test Scenarios

1. 3 nodes, 1 failure detector, 1 client — one node crashes and is detected.
2. 3 nodes, 1 failure detector, 1 client — all nodes remain alive, no false suspicions after stabilization.
3. 3 nodes, 1 failure detector, 1 client — two nodes crash at different times, both are eventually detected.
