# [System Name]

## Introduction

[One paragraph overview: what the system does, what problem it solves, and the core protocol or algorithm it implements.]

**Assumptions:**
1. [Assumption about communication model, e.g., reliable message delivery, no Byzantine faults]
2. [Assumption about failure model, e.g., crash-stop failures, no network partitions]
3. [Assumption about concurrency, e.g., requests are serialized, at most N concurrent clients]
4. [Assumption about reusable modules, e.g., "The Timer machine is a pre-existing reusable module — do NOT re-implement it. Use CreateTimer(this), StartTimer(timer), and CancelTimer(timer)."]

## Components

<!--
  Split into Source Components (protocol machines → PSrc/) and
  Test Components (test-only machines like clients/drivers → PTst/).
-->

### Source Components

#### 1. [MachineName]
- **Role:** [One-sentence role description.]
- **States:** [List known states, e.g., Init, WaitingForPrepare, Committed, Aborted]
- **Local state:**
    - `[variableName]`: [what it tracks]
    - `[variableName]`: [what it tracks]
- **Initialization:** [Describe in plain English what information this machine needs to start and how it receives it, e.g., "Created with a reference to all acceptors, all learners, and a unique proposer ID." or "Waits for a configuration event from the coordinator before becoming operational."]
- **Behavior:**
    - [Key behavior point, e.g., "Sends prepare requests to all participants and waits for responses."]
    - [Key behavior point, e.g., "Times out and aborts if responses are not received in time."]
- **Event handling notes:**
    - In [StateName]: ignore `[eStaleEvent1]`, `[eStaleEvent2]`
    - In [StateName]: defer `[eDeferredEvent1]`, `[eDeferredEvent2]`

#### 2. [MachineName]
- ...

### Test Components

#### 3. [ClientOrDriverName]
- **Role:** [One-sentence role description.]
- **States:** [List known states]
- **Local state:**
    - `[variableName]`: [what it tracks]
- **Initialization:** [Describe what information this machine needs to start, e.g., "Created with a reference to the server and the number of requests to send."]
- **Behavior:**
    - [Key behavior point, e.g., "Issues N write transactions with random key/value using choose()."]
    - [Key behavior point, e.g., "On success, performs a read and asserts the value matches."]

## Interactions

1. **[eEventName]**
    - **Source:** [MachineName]
    - **Target:** [MachineName(s)]
    - **Payload:** [Describe the data carried by this event in plain English, e.g., "the proposer's reference, the proposal number, and the proposed value"]
    - **Description:** [What this event represents and when it is sent.]
    - **Effects:**
        - [Effect on the receiver, including state transitions.]
        - [Any conditional behavior, e.g., "If the proposal number is higher than any previously seen..."]

2. **[eEventName]**
    - **Source:** [MachineName]
    - **Target:** [MachineName(s)]
    - **Payload:** none
    - **Description:** [What this event represents.]
    - **Effects:**
        - [Effect on the receiver.]

## Specifications

<!--
  Each property should have a descriptive name, its type (safety/liveness),
  and a precise English statement. Reference the relevant events naturally
  within the description so the LLM knows which events to observe.
-->

1. **[PropertyName]** (safety property):
   [Precise statement that naturally references the relevant events. E.g., "Whenever an eAccepted is sent for a value, no future eAccepted may carry a different value — only one value can ever be chosen."]

2. **[PropertyName]** (liveness property):
   [Precise statement that naturally references the relevant events. E.g., "If a majority of participants are alive, an eLearn event carrying the consensus value is eventually delivered."]

## Test Scenarios

<!--
  Concrete test scenarios with specific machine counts and expected behavior.
  Each scenario maps to a test case in PTst/.
-->

1. [N] [machine_type_1], [M] [machine_type_2], [K] [machine_type_3] — [description of what is being tested and expected outcome].
2. [N] [machine_type_1], [M] [machine_type_2] — [description of failure scenario, e.g., "one proposer fails mid-protocol, remaining machines still reach consensus"].
3. [Description of edge case or stress scenario].
