# PLean — Status

Current snapshot. Refresh when a phase flips or a major work item lands.
[`ROADMAP.md`](ROADMAP.md) lists the phases and what is next.

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
| [`PingPongTrivial`](../Examples/PingPongTrivial.lean) | 8 | 3 | 0 | 2 s | 1 s |
| [`PingPongAuto`](../Examples/PingPongAuto.lean) | 16 | 13 | 1 | 6 s | 1 s |
| [`DistributedLock`](../Examples/DistributedLock.lean) | 11 | 9 | 0 | 5 s | 2 s |
| [`ShardedKV`](../Examples/ShardedKV.lean) | 15 | 13 | 0 | 5 s | 2 s |
| [`LockServer`](../Examples/LockServer.lean) | 36 | 32 | 0 | 14 s | 2 s |
| [`RingLeader`](../Examples/RingLeader.lean) | 24 | 22 | 1 | 18 s | 5 s |
| [`Consensus`](../Examples/Consensus.lean) | 34 | 30 | 0 | 13 s | 5 s |
| [`TwoPhaseCommit`](../Examples/TwoPhaseCommit.lean) | 203 | 184 | 0 | 20 s | 7 s |
| [`ClockBound`](../Examples/ClockBound.lean) | 118 | 115 | 0 | 66 s | 8 s |
| [`Paxos`](../Examples/Paxos.lean) | 337 | 322 | 0 | 136 s | 26 s |
| [`FlexiblePaxos`](../Examples/FlexiblePaxos.lean) | 298 | 284 | 0 | 102 s | 21 s |
| [`MultiPaxos`](../Examples/MultiPaxos.lean) | 203 | 195 | 0 | 292 s | 122 s |

After an edit, only what it affects is proved again. Adding an invariant
to ClockBound's `topology` lemma sends 8 goals to the solver and takes
28 s; the rest is `vcgen` and normalisation of the obligations that
assume `topology`, whose other goals are reused. Removing it again sends
none (28 s).

Paxos, FlexiblePaxos and MultiPaxos are the only examples with manual
proofs. Paxos has four. One is that two majorities share an Acceptor
(`sizeof` is opaque to the solvers), a base case proved from
`PSet.quorum_intersect`. The other three are the steps in which a Proposer
completes a majority: each is a lemma about one world, proved by hand, that
the obligation hands to `pverify_auto`. FlexiblePaxos has two of that kind,
the steps in which a Proposer completes a phase-1 or a phase-2 quorum. Its
initial worlds assume that quorums of the two phases intersect and that
ballots are distinct, so neither needs a proof. MultiPaxos has five: the
steps in which a Proposer completes a majority of promises, proposes at a
slot, or completes a majority of votes, and the two obligations of the step
that merges a promise's votes into the ones recovered so far. Most of its
"Again" time goes to those five, since a manual proof is elaborated afresh
each time (`#pverify` itself takes 4 s). MultiPaxos is also the first
example with invariants about the values in maps and with sets stored in a
map. Its "From scratch" time is with the proof cache on and empty, where
about 40 obligations reuse goals proved for another target in the same run. With the
cache off (`pverify.cache false`) one earlier run did not finish in 50
minutes; at the end one Lean thread was in `simp` with no solver running,
and which obligation it was on was not tracked down.

An obligation that is not proved is reported with a counterexample when
z3 finds one (see [`ProofSkill.md`](ProofSkill.md)).

## Document index

| Document | Purpose |
|---|---|
| [`CLAUDE.md`](../CLAUDE.md) | Build cookbook, conventions, surface keywords |
| [`ROADMAP.md`](ROADMAP.md) | What is next |
| [`ProofSkill.md`](ProofSkill.md) | Writing inductive invariants, diagnosing obligations |
| [`STYLE.md`](STYLE.md) | Source / proof / tactic style rules |
