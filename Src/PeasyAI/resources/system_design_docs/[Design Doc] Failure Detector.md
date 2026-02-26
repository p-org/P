# Failure Detector

## Introduction

The goal of this system is to model an eventually perfect failure detector using heartbeat-based monitoring. Nodes periodically send heartbeats to a failure detector, which monitors node liveness and reports suspected failures to clients.

**Assumptions:**
1. Nodes can fail by stopping (crash failures).
2. The failure detector uses periodic heartbeat timeouts to detect failures.
3. The failure detector may temporarily suspect a live node (unreliable detection) but will eventually be accurate for permanently failed nodes.
4. Communication is reliable between live nodes.
5. A Timer machine models non-deterministic OS timer behavior. It provides helper functions: CreateTimer(client), StartTimer(timer), CancelTimer(timer). The Timer sends eTimeOut to its client when it fires.

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

#### 3. Timer
- **Role:** Models non-deterministic OS timer behavior for periodic timeouts.
- **States:** Init, WaitForTimerRequests, TimerStarted
- **Local state:**
    - `client`: the machine that created this timer (receives eTimeOut)
    - `numDelays`: counter tracking how many times the timer has delayed (bounded)
- **Initialization:** Created with a reference to the client machine via `CreateTimer(client)`.
- **Behavior:**
    - Waits for eStartTimer, then non-deterministically fires eTimeOut to the client.
    - The timer bounds the number of delays (e.g., max 3) so it is guaranteed to eventually fire. This is required for liveness properties. After `numDelays >= 3`, the timer must fire unconditionally.
    - Supports eCancelTimer to cancel a pending timeout.
    - Uses eDelayedTimeOut internally to model non-deterministic delay.
- **Helper functions (declared at file scope, outside the machine):**
    - `fun CreateTimer(client: machine) : Timer` — creates a new Timer for the given client
    - `fun StartTimer(timer: Timer)` — sends eStartTimer to the timer
    - `fun CancelTimer(timer: Timer)` — sends eCancelTimer to the timer

### Test Components

#### 4. Client
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

6. **eStartTimer**
    - **Source:** Any machine (via StartTimer helper)
    - **Target:** Timer
    - **Payload:** none
    - **Description:** Starts the timer. The Timer will eventually send eTimeOut to its client.

7. **eCancelTimer**
    - **Source:** Any machine (via CancelTimer helper)
    - **Target:** Timer
    - **Payload:** none
    - **Description:** Cancels a pending timer.

8. **eDelayedTimeOut**
    - **Source:** Timer (internal)
    - **Target:** Timer (self)
    - **Payload:** none
    - **Description:** Internal event used by Timer to model non-deterministic delay.

## Specifications

1. **ReliableDetection** (liveness property):
   If a node crashes (receives eCrash and stops sending eHeartbeat), the failure detector must **eventually** suspect that node (issue eNodeSuspected).

   This is a liveness property — it asserts that something good eventually happens. In P, express this using a **hot state** in the spec monitor:
   - When a node crashes, transition to a **hot** state.
   - A hot state means "the system must eventually leave this state" — PChecker will flag it as a liveness violation if the system stays in a hot state forever.
   - When the crashed node is suspected (eNodeSuspected received for that node), transition back to a cold (normal) state.
   - The spec must observe: eCrash, eHeartbeat, eNodeSuspected.
   - The spec must NOT observe eTimeOut. The eTimeOut event is shared by all Timer instances (Node timers and the FD timer), so the spec cannot distinguish which timer fired. Observing eTimeOut would cause the spec to incorrectly process Node timer events as FD timeout rounds.

## Test Scenarios

1. 3 nodes, 1 failure detector, 1 client — one node crashes and is detected.
2. 3 nodes, 1 failure detector, 1 client — two nodes crash at different times, both are eventually detected.
