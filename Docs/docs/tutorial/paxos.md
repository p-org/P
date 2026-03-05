## Example 5: Single Decree Paxos

How can we finish our tutorials on modeling distributed systems without giving tribute to the Paxos protocol :pray: (and our inspiration [Leslie Lamport](http://www.lamport.org/)). Let's end the tutorial with a simplified **[single decree paxos](https://mwhittaker.github.io/blog/single_decree_paxos/)**.

??? note "How to use this example"

    We assume that you have cloned the P repository locally.
    ```shell
    git clone https://github.com/p-org/P.git
    ```

    The recommended way to work through this example is to open the [P/Tutorial](https://github.com/p-org/P/tree/master/Tutorial) folder in [Peasy IDE](../getstarted/PeasyIDE.md) (VS Code extension) or your preferred editor side-by-side with a browser, so you can simultaneously read the description for each example and browse the P program.

    To know more about P language primitives used in the example, please look them up in the [language manual](../manualoutline.md).

**System:** We present a simplified model of the [single decree Paxos](https://mwhittaker.github.io/blog/single_decree_paxos/) consensus protocol. In single decree Paxos, a set of proposers attempt to get a single value agreed upon (decided) by a set of acceptors, and the decided value is then taught to a set of learners. The protocol guarantees that **at most one value is ever decided**, even in the presence of concurrent proposers and unreliable networks.

We say simplified because general Paxos is resilient against arbitrary network behavior (lossy, duplicate, re-order, and delay). In our model, we model message loss and delay, and check correctness of Paxos in the presence of such a network. This is a fun exercise — we encourage you to play around and create variants of Paxos!

**Correctness Specification:** The safety property we check is that once a value is decided (taught to a learner), all subsequent decisions must agree on the same value. This is the core consensus guarantee of Paxos.

!!! abstract "What will we learn?"
    Modeling a classic consensus protocol as communicating state machines, using nondeterminism to model unreliable networks (message loss and duplication), writing a consensus safety specification, and testing with multiple configurations of proposers and acceptors.

---

### P Project

The [5_Paxos](https://github.com/p-org/P/tree/master/Tutorial/5_Paxos) folder contains the source code for this example. The P project file for the Paxos example is available [here](https://github.com/p-org/P/blob/master/Tutorial/5_Paxos/SingleDecreePaxos.pproj).

The Paxos protocol has three roles — **Proposer**, **Acceptor**, and **Learner** — each modeled as a P state machine:

| Role | Machine | Description |
|------|---------|-------------|
| **Proposer** | [`PSrc/proposer.p`](https://github.com/p-org/P/blob/master/Tutorial/5_Paxos/PSrc/proposer.p) | Drives the protocol through three phases: Prepare, Accept, and Teach |
| **Acceptor** | [`PSrc/acceptor.p`](https://github.com/p-org/P/blob/master/Tutorial/5_Paxos/PSrc/acceptor.p) | Promises not to accept lower ballots and accepts values from valid leaders |
| **Learner** | [`PSrc/learner.p`](https://github.com/p-org/P/blob/master/Tutorial/5_Paxos/PSrc/learner.p) | Receives decided values and asserts consistency |

---

### Models

#### Types and Events

The protocol communication is defined through ballot numbers, values, and four message types ([`PSrc/acceptor.p`](https://github.com/p-org/P/blob/master/Tutorial/5_Paxos/PSrc/acceptor.p)):

```
type tBallot = int;
type tValue = int;

event ePrepareReq: tPrepareReq;    // Proposer → Acceptor (Phase 1a)
event ePrepareRsp: tPrepareRsp;    // Acceptor → Proposer (Phase 1b)
event eAcceptReq: tAcceptReq;      // Proposer → Acceptor (Phase 2a)
event eAcceptRsp: tAcceptRsp;      // Acceptor → Proposer (Phase 2b)
event eLearn: (ballot: tBallot, v: tValue);  // Proposer → Learner (Phase 3)
```

#### Acceptor Machine

The Acceptor ([`PSrc/acceptor.p`](https://github.com/p-org/P/blob/master/Tutorial/5_Paxos/PSrc/acceptor.p)) tracks three pieces of state: the highest ballot it has promised (`n_prepared`), the value it has accepted (`v_accepted`), and the ballot number of the accepted value (`n_accepted`).

```
machine Acceptor {
    var n_prepared: tBallot;
    var v_accepted: tValue;
    var n_accepted: tBallot;

    start state Init {
        entry {
            n_prepared = -1;
            v_accepted = -1;
            n_accepted = -1;
            goto Accept;
        }
    }

    state Accept {
        on ePrepareReq do (req: tPrepareReq) {
            if (req.ballot_n > n_prepared) {
                send req.proposer, ePrepareRsp,
                    (acceptor = this, promised = req.ballot_n,
                     v_accepted = v_accepted, n_accepted = n_accepted);
                n_prepared = req.ballot_n;
            }
        }

        on eAcceptReq do (req: tAcceptReq) {
            if (req.ballot_n >= n_prepared) {
                v_accepted = req.v;
                n_accepted = req.ballot_n;
                n_prepared = req.ballot_n;
                send req.proposer, eAcceptRsp,
                    (acceptor = this, accepted = req.ballot_n);
            }
        }
    }
}
```

!!! note "Design Decision"
    The Acceptor treats an accept as an implicit prepare (following Lamport's "Part Time Parliament" formulation), updating `n_prepared` on accept. This simplifies the protocol without affecting correctness.

#### Proposer Machine

The Proposer ([`PSrc/proposer.p`](https://github.com/p-org/P/blob/master/Tutorial/5_Paxos/PSrc/proposer.p)) drives the three phases of the protocol as P states:

- **Prepare** (Phase 1): Sends `ePrepareReq` to all acceptors. Collects promises until a majority responds. If any acceptor has already accepted a value, adopts the value with the highest ballot number.
- **Accept** (Phase 2): Sends `eAcceptReq` with the chosen value to all acceptors. Waits for a majority of accept acknowledgments.
- **Teach** (Phase 3): Sends `eLearn` to all learners with the decided value.

```
machine Proposer {
    var jury: set[Acceptor];
    var school: set[Learner];
    var ballot_n: tBallot;
    var value_to_propose: tValue;
    var majority: int;
    var prepare_acks: set[Acceptor];
    var accept_acks: set[Acceptor];
    var highest_proposal_n: tBallot;

    start state Init {
        entry (cfg: tProposerConfig) {
            jury = cfg.jury;
            school = cfg.school;
            ballot_n = cfg.proposer_id;
            value_to_propose = cfg.value_to_propose;
            majority = sizeof(jury) / 2 + 1;
            goto Prepare;
        }
    }

    state Prepare {
        entry {
            var acceptor: Acceptor;
            highest_proposal_n = -1;
            foreach (acceptor in jury) {
                send acceptor, ePrepareReq,
                    (proposer = this, ballot_n = ballot_n, v = value_to_propose);
            }
        }

        on ePrepareRsp do (rsp: tPrepareRsp) {
            if (rsp.promised == ballot_n) {
                if (rsp.n_accepted > highest_proposal_n) {
                    highest_proposal_n = rsp.n_accepted;
                    value_to_propose = rsp.v_accepted;
                }
                prepare_acks += (rsp.acceptor);
                if (sizeof(prepare_acks) >= majority) {
                    goto Accept;
                }
            }
        }
    }

    state Accept { ... }
    state Teach { ... }
}
```

!!! tip "Key Insight"
    The Proposer uses **sets** (not counters) to track acknowledgments. This correctly handles duplicate message delivery — if the same acceptor's response arrives twice, the set size doesn't change.

#### Learner Machine

The Learner ([`PSrc/learner.p`](https://github.com/p-org/P/blob/master/Tutorial/5_Paxos/PSrc/learner.p)) is the simplest role. It receives decided values and asserts that the value never changes:

```
machine Learner {
    var learned_value: tValue;

    start state Init {
        entry {
            learned_value = -1;
            goto Learn;
        }
    }

    state Learn {
        on eLearn do (payload: (ballot: tBallot, v: tValue)) {
            assert(payload.v != -1);
            assert((learned_value == -1) || (learned_value == payload.v));
            learned_value = payload.v;
        }
    }
}
```

---

### Modeling Unreliable Networks

A key aspect of this example is modeling network unreliability. The file [`PSpec/common.p`](https://github.com/p-org/P/blob/master/Tutorial/5_Paxos/PSpec/common.p) provides broadcast utility functions that use P's `choose()` to model nondeterminism:

| Function | Behavior |
|----------|----------|
| `UnreliableBroadcast` | Each message may or may not be delivered (`choose()` for each) |
| `UnreliableBroadcastMulti` | Each message may be delivered 0 to 3 times (`choose(3)`) |
| `ReliableBroadcast` | All messages are delivered exactly once |
| `ReliableBroadcastMajority` | Reliable delivery to a majority, unreliable to the rest |

!!! note ""
    The `choose()` expression returns a nondeterministic boolean. The P checker systematically explores both `true` and `false` branches, effectively testing all possible network behaviors.

---

### Specifications

The safety specification ([`PSpec/spec.p`](https://github.com/p-org/P/blob/master/Tutorial/5_Paxos/PSpec/spec.p)) is the **OneValueTaught** monitor. It observes all `eLearn` events and asserts that once a value is decided, all subsequent decisions agree on the same value:

```
spec OneValueTaught observes eLearn {
    var decided: int;

    start state Init {
        entry {
            decided = -1;
        }

        on eLearn do (payload: (ballot: tBallot, v: tValue)) {
            assert(payload.v != -1);
            if (decided != -1) {
                assert(decided == payload.v);
            }
            decided = payload.v;
        }
    }
}
```

This captures the core consensus safety property: **agreement** — no two learners learn different values.

---

### Test Scenarios

The test file ([`PTst/test.p`](https://github.com/p-org/P/blob/master/Tutorial/5_Paxos/PTst/test.p)) defines several configurations with varying numbers of proposers and acceptors:

| Test Case | Proposers | Acceptors | Learners |
|-----------|-----------|-----------|----------|
| `testBasicPaxos1on1` | 1 | 1 | 1 |
| `testBasicPaxos2on2` | 2 | 2 | 1 |
| `testBasicPaxos2on3` | 2 | 3 | 1 |
| `testBasicPaxos3on1` | 3 | 1 | 1 |
| `testBasicPaxos3on3` | 3 | 3 | 1 |
| `testBasicPaxos3on5` | 3 | 5 | 1 |

Each test case creates the machines and asserts the `OneValueTaught` specification. The proposers are initialized with nondeterministic values (`choose(50)`) and ballot numbers, so the checker explores many different orderings and value choices.

---

### Compiling and Checking

Navigate to the Paxos example folder and compile:

```shell
cd Tutorial/5_Paxos
p compile
```

Run the checker on a test case:

```shell
p check -tc testBasicPaxos3on5 -s 1000
```

!!! tip "Try different configurations"
    The `testBasicPaxos3on5` test (3 proposers, 5 acceptors) is the most interesting because it exercises concurrent proposals with a realistic quorum size. Try increasing the number of schedules (`-s`) to explore more behaviors.

---

### Discussion

!!! info "Why sets for ACK tracking?"
    In our network model, messages can be delivered multiple times (via `UnreliableBroadcastMulti`). Using a `set[Acceptor]` instead of a counter for tracking acknowledgments means that duplicate responses from the same acceptor don't inflate the count. This is a common pattern when modeling protocols over unreliable networks in P.

!!! info "Extending the model"
    Some interesting extensions to try:

    - Add message re-ordering by introducing an intermediate network machine
    - Model proposer failures (a proposer crashes mid-protocol)
    - Add a liveness specification: if a single proposer is eventually the only one proposing, a value is eventually decided
    - Implement multi-decree Paxos (Multi-Paxos) by adding a log of decisions

---

!!! success "What did we learn through this example?"
    We modeled the classic single decree Paxos consensus protocol as communicating P state machines. We used `choose()` to model unreliable network behavior (message loss and duplication), wrote a consensus safety specification (`OneValueTaught`), and tested with multiple configurations of proposers and acceptors. The P checker systematically explores different interleavings and network behaviors to verify that the protocol maintains agreement.
