# PLean — Status

Current snapshot. Refresh when a phase checkbox flips or a major work
item lands. Historical sessions, decision rationale, and workstream
planning live in [`ROADMAP.md`](ROADMAP.md).

## At a Glance

| Phase | Status | Notes |
|---|---|---|
| 0 — Bootstrap                    | ☑ | Lake skeleton; multi-file `pmodule M`; `#pwf` / `#pverify` shell |
| 1 — Semantic core                | ☑ | `PM := NonDetT (StateT (GlobalState Sig) DivM)`; primitives; default invariants |
| 2 — Registry + surface           | ☑ | `#gen_module M`; surface macros over the real PM |
| 3 — Verification declarations    | ☑ | `Lemma`/`Theorem`/`Proof`/`system <σ>`; SMT-discharge `#pverify`; `@[pverifyProof]`; `paxiom` / `pinstance` axiom bridge |
| 4 — Spec machines                | ☐ | next; see [`ROADMAP.md` § W3](ROADMAP.md) |
| 5 — Remaining surface            | ◐ | container types (`set` / `map` / `seq` / `option`) and `foreach` / `while` with invariants shipped; `assume` and loop-aware `default_inv` pending |
| 6 — Tutorial port                | ☐ | `1_ClientServer`, `2_TwoPhaseCommit` |
| 7 — Stretch                      | ⊘ | post-v1 |

Build green at HEAD.

## Closure rates

| Benchmark | Obligations | Notes |
|---|---|---|
| [`Examples/DistributedLock`](../Examples/DistributedLock.lean) | **12 / 12**  (all SMT) | |
| [`Examples/LockServer`](../Examples/LockServer.lean)           | **37 / 37**  (34 SMT + 3 manual) | manual proofs are send-handler bundles where lean-auto rejects single-shot |
| [`Examples/RingLeader`](../Examples/RingLeader.lean)           | **17 / 17**  (14 SMT + 3 manual) | manual proofs are `goto Won` inductive steps + entry handler |
| [`Examples/ClockBound`](../Examples/ClockBound.lean)           | **59 / 59**  (all SMT) | off-tree AWS clock-bound daemon port; exercises `PLean.choose` |
| [`Examples/ShardedKV`](../Examples/ShardedKV.lean)             | **11 / 11**  (all SMT) | exercises `map[K, V]` + multi-ref `unique_owner` |
| [`Examples/Consensus`](../Examples/Consensus.lean)             | **23 / 23**  (17 SMT + 5 manual + 1 axiom) | toy Paxos-style election-safety under `unique_quorum` |

## In flight

Phase 3 (M3) closed. Phase 4 (spec machines) is the next critical-path
work — see [`ROADMAP.md` § W3](ROADMAP.md).

Phase 5 surface gaps that block other benchmarks:
- loop-aware `default_inv` so an auto-emitted `prove default;` under a
  `foreach` / `while` body closes without a hand-written
  `@[pverifyProof]` invoking `triple_pforeach_with`;
- `assume <prop>;` statement surface.

## Document index

| Document | Purpose |
|---|---|
| [`CLAUDE.md`](../CLAUDE.md)              | Build cookbook, conventions, surface keywords |
| [`ROADMAP.md`](ROADMAP.md)               | Contributor-facing progress, workstreams, sprint allocation |
| [`ProofSkill.md`](ProofSkill.md)         | Manual-proof workflow + SMT pitfalls |
| [`STYLE.md`](STYLE.md)                   | Source / proof / tactic style rules |
| [`REVIEW.md`](REVIEW.md)                 | Code-review audit driving recent usability / soundness work |
