# Raft Leader Election

## Introduction

The goal of this system is to model the leader election component of the Raft consensus protocol. In Raft, a cluster of servers elects a single leader which is responsible for managing the replicated log. If the leader fails, a new election is triggered. This models only the election mechanism, not log replication.

**Assumptions:**
1. Servers communicate via reliable message passing.
2. Each server starts as a Follower.
3. If a Follower does not hear from a leader within a timeout, it becomes a Candidate and starts an election.
4. A Candidate requests votes from all other servers. A server grants its vote to the first valid candidate in a given term.
5. A Candidate that receives votes from a majority becomes the Leader.
6. If a Candidate discovers a higher term, it reverts to Follower.
7. The Leader sends periodic heartbeats to maintain authority.
8. The Timer machine is a pre-existing reusable module — do NOT re-implement it. Use CreateTimer(this), StartTimer(timer), and CancelTimer(timer).

## Components

### Source Components

#### 1. Server
- **Role:** Participates in leader election, cycling through Follower, Candidate, and Leader roles.
- **States:** Follower, Candidate, Leader
- **Local state:**
    - `currentTerm`: the current election term
    - `votedFor`: which candidate this server voted for in the current term
    - `peers`: references to all other servers
    - `voteCount`: number of votes received as a candidate
    - `majoritySize`: number needed for a majority
    - `electionTimer`: timer for election timeout
    - `heartbeatTimer`: timer for heartbeat interval
- **Initialization:** Created with references to all peer servers and a unique server ID.
- **Behavior:**
    - As Follower: waits for heartbeats; starts election on timeout.
    - As Candidate: increments term, votes for self, sends RequestVote to all peers.
    - As Leader: sends periodic heartbeats (AppendEntries with no data) to all peers.
- **Event handling notes:**
    - In Leader: ignore `eVoteResponse` (stale from election phase)
    - In Follower: ignore `eVoteResponse`

#### 2. Timer
- **Role:** A generic timer machine that sends eTimeOut after a configurable period.
- **Initialization:** Pre-existing reusable module. Created by the server that owns it.
- **Behavior:**
    - Used for election timeouts (followers/candidates) and heartbeat intervals (leaders).

### Test Components

#### 3. TestDriver
- **Role:** Creates the cluster of servers and optionally injects failures.
- **Initialization:** No configuration needed. Creates the cluster of servers directly.
- **Behavior:**
    - Creates N servers, each with references to all peers.
    - Optionally stops a leader to trigger re-election.

## Interactions

1. **eRequestVote**
    - **Source:** Candidate Server
    - **Target:** All peer Servers
    - **Payload:** the candidate's reference and its current term
    - **Description:** Candidate requests a vote from a peer.
    - **Effects:**
        - If the receiver's term is less than or equal and it hasn't voted yet in this term, it grants its vote.
        - If the receiver's term is higher, the candidate updates its term and reverts to follower.

2. **eVoteResponse**
    - **Source:** Peer Server
    - **Target:** Candidate Server
    - **Payload:** the responder's current term and whether the vote was granted
    - **Description:** Server responds to a vote request.
    - **Effects:**
        - If majority votes received, candidate becomes leader.
        - If term in response is higher, candidate reverts to follower.

3. **eAppendEntries**
    - **Source:** Leader Server
    - **Target:** Follower Server
    - **Payload:** the leader's term and the leader's reference
    - **Description:** Leader heartbeat to maintain authority.
    - **Effects:**
        - Follower resets its election timer.
        - If leader's term >= follower's term, follower acknowledges the leader.
        - If leader's term < follower's term, follower rejects (stale leader).

4. **eAppendEntriesResponse**
    - **Source:** Follower Server
    - **Target:** Leader Server
    - **Payload:** the follower's current term and whether the heartbeat was accepted
    - **Description:** Follower acknowledges or rejects the heartbeat.

5. **eTimeOut**
    - **Source:** Timer
    - **Target:** Server
    - **Payload:** none
    - **Description:** Timer expiration triggers election (for followers/candidates) or heartbeat sending (for leaders).

## Specifications

1. **AtMostOneLeaderPerTerm** (safety property):
   In any given term, at most one server may transition to the Leader role. Tracking eRequestVote and eVoteResponse exchanges ensures no two servers collect a majority in the same term, and eAppendEntries heartbeats must never originate from two different leaders in the same term.

## Test Scenarios

1. 3 servers, one becomes leader after initial election with no contention.
2. 5 servers, leader crashes (stops heartbeating), a new leader is elected.
3. 3 servers with split vote — no majority in first round, re-election succeeds.
