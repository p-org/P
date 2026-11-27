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
| P4 Obligations | ☑ | counterexamples for obligations that are not proved; parallel, incremental runs; manual proofs |
| P5 Ports | ☑ | every example verifies |
| P6 Explicit-state BMC | ☐ | backlog |

## Closure

Every obligation of every example is proved. Those not counted under
"With an SMT solver" were closed by `vcgen` and normalisation (or their
goals are hypotheses). "Trusted" obligations were proved by the solver
but its proof could not be replayed in Lean; both have checked proofs too
(RingLeader's with `pverify.trust "reconstruct"`, PingPongAuto's by hand).
Times are the wall-clock time of checking the example's file, ten
obligations at a time on one machine: from an empty proof cache, and
again with nothing changed (every proof replayed through the kernel).

| Example | Obligations | With an SMT solver | Trusted | From scratch | Again |
|---|---|---|---|---|---|
| [`PingPongTrivial`](../Examples/PingPongTrivial.lean) | 8 | 3 | 0 | 2 s | 2 s |
| [`PingPongAuto`](../Examples/PingPongAuto.lean) | 16 | 13 | 1 | 6 s | 2 s |
| [`DistributedLock`](../Examples/DistributedLock.lean) | 11 | 9 | 0 | 8 s | 2 s |
| [`ShardedKV`](../Examples/ShardedKV.lean) | 15 | 13 | 0 | 7 s | 2 s |
| [`LockServer`](../Examples/LockServer.lean) | 36 | 32 | 0 | 24 s | 3 s |
| [`RingLeader`](../Examples/RingLeader.lean) | 24 | 22 | 1 | 21 s | 5 s |
| [`Consensus`](../Examples/Consensus.lean) | 34 | 30 | 0 | 70 s | 5 s |
| [`TwoPhaseCommit`](../Examples/TwoPhaseCommit.lean) | 203 | 184 | 0 | 23 s | 6 s |
| [`ClockBound`](../Examples/ClockBound.lean) | 118 | 115 | 0 | 110 s | 8 s |

After an edit, only what it affects is proved again. Adding an invariant
to ClockBound's `topology` lemma sends 8 goals to the solver and takes
52 s; the rest is `vcgen` and normalisation of the obligations that
assume `topology`, whose other goals are reused. Removing it again sends
none (49 s).

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
