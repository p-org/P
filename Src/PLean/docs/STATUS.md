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
| P4 Obligations | ☑ | counterexamples for obligations that are not proved |
| P5 Ports | ☑ | every example verifies |
| P6 Explicit-state BMC | ☐ | backlog |

## Closure

Every obligation of every example is proved. Those not counted under
"With an SMT solver" were closed by `vcgen` and normalisation (or their
goals are hypotheses). "Trusted" obligations were proved by the solver
but its proof could not be replayed in Lean.
Times are the total solving time of `#pverify` on one machine.

| Example | Obligations | With an SMT solver | Trusted | Time |
|---|---|---|---|---|
| [`PingPongTrivial`](../Examples/PingPongTrivial.lean) | 8 | 3 | 0 | 1 s |
| [`PingPongAuto`](../Examples/PingPongAuto.lean) | 16 | 12 | 1 | 7 s |
| [`DistributedLock`](../Examples/DistributedLock.lean) | 11 | 8 | 0 | 12 s |
| [`ShardedKV`](../Examples/ShardedKV.lean) | 11 | 8 | 0 | 11 s |
| [`LockServer`](../Examples/LockServer.lean) | 36 | 29 | 0 | 82 s |
| [`RingLeader`](../Examples/RingLeader.lean) | 19 | 15 | 1 | 81 s |
| [`Consensus`](../Examples/Consensus.lean) | 27 | 22 | 0 | 110 s |
| [`TwoPhaseCommit`](../Examples/TwoPhaseCommit.lean) | 203 | 183 | 0 | 81 s |
| [`ClockBound`](../Examples/ClockBound.lean) | 118 | 112 | 0 | 514 s |

No example needs a manual proof. An obligation that is not proved is
reported with a counterexample when z3 finds one (see
[`ProofSkill.md`](ProofSkill.md)).

## Document index

| Document | Purpose |
|---|---|
| [`CLAUDE.md`](../CLAUDE.md) | Build cookbook, conventions, surface keywords |
| [`TransitionModel.md`](TransitionModel.md) | The design |
| [`ROADMAP.md`](ROADMAP.md) | What is next |
| [`ProofSkill.md`](ProofSkill.md) | Writing inductive invariants, diagnosing obligations |
| [`STYLE.md`](STYLE.md) | Source / proof / tactic style rules |
