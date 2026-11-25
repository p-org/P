# PLean — Status

Current snapshot. Refresh when a phase flips or a major work item lands.
The phases are those of [`TransitionModel.md`](TransitionModel.md); see
[`ROADMAP.md`](ROADMAP.md) for what is next.

## At a glance

| Phase | Status | Notes |
|---|---|---|
| P0 Toolchain | ☑ | Lean v4.36.0; lean-crush is the only dependency |
| P1 Prototype | ☑ | |
| P2 Semantics library | ☑ | metatheory proved, no `sorry` |
| P3 Code generation | ☑ | `receive` as a top-level statement of a handler / entry body |
| P4 Obligations | ☑ | |
| P5 Ports | ☑ | every example verifies |
| P6 Explicit-state BMC | ☐ | backlog |

## Closure

Every obligation of every example is proved. "Trusted" obligations were
proved by the solver but its proof could not be replayed in Lean.
Times are the total solving time of `#pverify` on one machine.

| Example | Obligations | Trusted | Time |
|---|---|---|---|
| [`PingPongTrivial`](../Examples/PingPongTrivial.lean) | 8 | 0 | 1 s |
| [`PingPongAuto`](../Examples/PingPongAuto.lean) | 16 | 1 | 10 s |
| [`DistributedLock`](../Examples/DistributedLock.lean) | 11 | 0 | 18 s |
| [`ShardedKV`](../Examples/ShardedKV.lean) | 11 | 0 | 15 s |
| [`LockServer`](../Examples/LockServer.lean) | 36 | 0 | 108 s |
| [`RingLeader`](../Examples/RingLeader.lean) | 19 | 1 | 87 s |
| [`Consensus`](../Examples/Consensus.lean) | 27 | 0 | 107 s |
| [`TwoPhaseCommit`](../Examples/TwoPhaseCommit.lean) | 203 | 0 | 116 s |
| [`ClockBound`](../Examples/ClockBound.lean) | 118 | 0 | 524 s |

No example needs a manual proof.

## Document index

| Document | Purpose |
|---|---|
| [`CLAUDE.md`](../CLAUDE.md) | Build cookbook, conventions, surface keywords |
| [`TransitionModel.md`](TransitionModel.md) | The design |
| [`ROADMAP.md`](ROADMAP.md) | What is next |
| [`ProofSkill.md`](ProofSkill.md) | Writing inductive invariants, diagnosing obligations |
| [`STYLE.md`](STYLE.md) | Source / proof / tactic style rules |
