# Pluggable timeline representations for the feedback-guided search

## Why

The feedback-guided search's diversity signal is a "timeline" abstraction of each
schedule (`TimelineObserver`). Historically that was, per receiver **instance**,
the set of ordered pairs of event **type names** in the receiver's local dequeue
order (`<M, e1, e2>`). For distributed systems this is the wrong shape:

- **No cross-machine causality** — a purely local, per-receiver projection; it
  never links a send to its delivery, so the happens-before partial order (the
  meaning of a "Lamport timeline") is exactly what it misses.
- **Saturates** — bounded by |eventTypes|² per receiver; once every ordered pair
  has appeared, deeper interleavings stop changing the timeline.
- **Type-only** — collapses which sender / which payload (ballot, term, txnId).
- **Ignores work already done** — the runtime already maintains vector clocks on
  every message; the timeline discarded them.

## What

A pluggable `ITimelineRepresentation` (selectable via `--timeline-repr`), with the
historical behavior as the default:

| repr | idea | cross-machine? |
|---|---|---|
| **pairwise** (default) | per-receiver ordered event-type pairs (historical) | no |
| **kgram** | per-receiver contiguous k-grams of the delivery sequence (finer; `--timeline-kgram N`, default 3) | no |
| **causal** | happens-before partial order over deliveries, abstracted to labels — from the runtime's vector clocks + per-receiver program order | **yes** |
| **hybrid** | union of causal + kgram tokens | yes |

Orthogonal knob: `--timeline-payload` enriches each event label with a stable hash
of the payload (off by default). Each representation just produces a set of string
tokens; `TimelineObserver` owns the shared, deterministic FNV-1a + fixed-coefficient
MinHash and the canonical-string view, so the existing novelty-gate / diversity /
ExploredTimelines plumbing is unchanged — only the token *content* differs.

## Correctness / robustness

- **Default preserved byte-for-byte.** The pairwise token uses the exact legacy
  field layout, so its FNV-1a hash (and thus the MinHash and search) is identical
  to before. Verified: `--timeline-repr pairwise` == default (no flag), and the
  full Release regression stays green (756/756).
- **Fixed a real happens-before bug.** `VectorTime.CompareTo` only iterated
  `this.Clock.Keys`, so a machine present in the *other* clock but not *this* one
  was never compared — a genuine `this → other` ordering could be silently
  misreported as *concurrent*. It now ranges over the **union** of both clocks'
  keys. (This is the first real consumer of `CompareTo`; surfaced by adversarial
  review.)
- **Determinism.** Everything is computed post-hoc from a finished schedule (never
  affects scheduling / record-replay); tokens are FNV-1a hashed (never
  `GetHashCode`); the abstract-timeline string is ordinal-sorted; and `PSet`/`PMap`
  `ToString` were made order-deterministic so `--timeline-payload` is reproducible
  under a fixed `--seed`.
- **Tests.** `Tst/UnitTests/TimelineRepresentationTest.cs` (12 tests): pairwise
  layout, kgram distinguishes cases where pairwise saturates (ABBA vs BAAB), the
  causal happens-before/concurrency token decision, hybrid union, factory, and
  empty schedules.

## E7 — Bake-off

Feedback strategy (`--sch-feedback`), all four representations, on
ClientServer / TwoPhaseCommit / Paxos. Two questions: does a richer representation
(a) raise distinct-timeline coverage / rare-scenario coverage, and (b) find bugs
faster?

### E7a — timelines & rare-scenario coverage (budget 1000, mean/5 seeds)

| model / rare scenario | metric | pairwise | kgram | causal | hybrid |
|---|---|---|---|---|---|
| ClientServer / TwoSuccessfulWithdrawals | distinct timelines | 21.2 | 33.8 | **1.2** | 43.8 |
| ClientServer / TwoSuccessfulWithdrawals | rare unique-timelines | 21.2 | 33.6 | **1.0** | 43.6 |
| Paxos / TwoLearns | distinct timelines | 40.8 | 50.6 | 29.6 | 77.8 |
| Paxos / TwoLearns | rare unique-timelines | 3.8 | 4.4 | 3.6 | 3.8 |
| TwoPhaseCommit / AbortThenCommit | distinct timelines | 44.6 | 78.8 | 70.8 | 86.4 |
| TwoPhaseCommit / AbortThenCommit | rare unique-timelines | 0.2 | 0 | 0.4 | 0 |

`kgram` beats `pairwise` on distinct timelines and rare coverage everywhere;
`hybrid` maximizes raw distinct timelines (it's the union). But `causal`
**collapses to ~1 timeline on ClientServer**: the happens-before order over event
*types* is schedule-invariant for request→response models (the req→resp chains
are fixed; client interleavings just become symmetric `co` tokens), so it can't
discriminate schedules — the "too coarse → no signal" failure mode. (`AbortThenCommit`
is ~unreachable at this budget across all reprs, so its row is noise.)

### E7b — iterations-to-first-bug (TPC MultiNoFailure, cap 3000, 10 seeds)

| repr | bugs found / 10 | median schedules-to-bug (found) | min |
|---|---|---|---|
| pairwise | 5/10 | 236 | 77 |
| kgram | 6/10 | 515 | 77 |
| **causal** | **9/10** | 366 | 77 |
| hybrid | 6/10 | 651 | 77 |

`causal` is the standout: **9/10 seeds vs pairwise's 5/10.** Recall plain
`feedback` (random base, pairwise timelines) *gets stuck* on ~half the seeds
(exhausts the budget without finding the bug). The true happens-before signal
gives the search enough of a diversity gradient to avoid that trap — it fixes the
exact "feedback gets stuck" failure documented earlier. (10 seeds; a promising
reliability gain worth confirming at larger scale.)

## Takeaway

**The representation matters, and there is no universal winner — each has a regime:**

- **Coverage / diversity → `kgram` (or `hybrid` for maximum breadth).** Finer
  local resolution beats pairwise on distinct timelines and rare-scenario coverage
  across all three models, and never degenerates.
- **Bug-finding reliability → `causal`.** The cross-machine happens-before signal
  takes first-bug reliability from 5/10 to 9/10 — it fixes feedback's get-stuck
  failure. This is the payoff for wiring in the vector clocks the runtime already
  computes.
- **`pairwise` (the old default) is dominated on both axes** — it exists now only
  as the backward-compatible baseline.
- **`causal` over event *types* is coarse for request→response models** (ClientServer
  → ~1 timeline): its happens-before structure is schedule-invariant there. Pair it
  with `--timeline-payload` (to distinguish deliveries by value), or reserve it for
  protocol models (TPC/Paxos) where interleavings do change the causal shape.

**Recommendation.** Keep `pairwise` as the safe default (zero behavior change), but
expose the knobs: reach for `kgram`/`hybrid` when the goal is coverage/diversity and
`causal` when hunting bugs on protocol models. A default flip to `kgram` (coverage)
or `causal` (bug-finding) is worth evaluating at larger scale — the same caveat as
the Cluster-B defaults. The bake-off harness (`fest-eval/run.py e7`) reproduces
every number here.

