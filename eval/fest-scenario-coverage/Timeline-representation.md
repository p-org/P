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

## Bake-off (definitive: multi-bug, larger model, random reference)

The feedback representations vs a plain-`random` reference, on **4 known bugs** (3
protocols, safety + liveness) at **50 seeds / cap 10000**, and on **coverage across 4
models including the larger Raft** (12–20 seeds). Every number was independently
re-audited from the raw CSVs. **This supersedes an earlier feedback-only run** whose
"causal is the best bug-finder" headline *did not hold* once plain `random` was in the
comparison. (The `+payload` variants are excluded — an earlier run showed whole-payload
hashing is "too fine" and degenerates the search; it needs selective-field hashing.)

### Iterations-to-first-bug (50 seeds, cap 10000, stop at first bug)

*Safety bugs — all strategies find them 100% (0% censored); latency (median schedules):*

| bug (kind) | random | feedback (all 4 reprs, identical) |
|---|---|---|
| ClientServer `guard>=0` (trivial safety) | 1 | 1 |
| Paxos `quorum-1` (safety) | **24.5** | 103 (4.2× slower) |
| TPC `commit N-1` (safety) | **19** | 104 (5.5× slower) |

*Liveness bug — TPC `Progress` (the only discriminating one):*

| strategy | found / 50 | found % | censored % | all-seed median |
|---|---|---|---|---|
| **random** | **50/50** | **100%** | 0% | **331** |
| feedback+causal | 47/50 | 94% | 6% | 1755 |
| feedback+pairwise | 38/50 | 76% | 24% | 3073 |
| feedback+hybrid | 36/50 | 72% | 28% | 3899 |
| feedback+kgram | 28/50 | 56% | 44% | 7385 |

- **Plain `random` dominates every feedback variant on every bug.** On safety bugs it is
  4–5× faster (feedback spends budget *exploiting* instead of sampling broadly). On the
  liveness bug it is both the fastest (331 vs ≥1755) and the most reliable (100%).
- **For safety bugs the timeline representation is immaterial** — all four feedback
  reprs have *identical* medians (103/104): the bug is found in the repr-independent
  early exploration phase, before the diversity signal diverges.
- **Among feedback variants, only the liveness bug discriminates them**: causal (94%) >
  pairwise (76%) > hybrid (72%) > kgram (56%). causal is **significantly** better than
  kgram (Fisher p<0.001); its edge over pairwise (p=0.023) and hybrid (p=0.006) is
  modest and hinges on a few seeds. **`kgram` is the weakest.** But even causal does not
  beat `random` — the 100%-vs-94% gap is *not* significant (Fisher p=0.242).

### Coverage — distinct timelines by representation (feedback, incl. larger Raft)

| model / test case | pairwise | kgram | causal | hybrid |
|---|---|---|---|---|
| ClientServer/tcMultipleClients | 21.8 | 37.4 | **1.4** | 47.9 |
| Paxos/testBasicPaxos3on5 | 50.0 | 56.8 | 32.7 | 89.3 |
| TwoPhaseCommit/tcMultipleClientsNoFailure | 121.0 | 78.5 | **168.1** | 109.7 |
| Raft/oneClientThreeServersReliable (larger) | 80.5 | 50.9 | **99.2** | 50.9 |

- **Coverage ranking does not generalize.** `causal` is *worst* on the two small models
  (ClientServer 1.4, Paxos 32.7 — the type-level happens-before is schedule-invariant
  there) yet *best* on the two larger/more-concurrent models (TPC 168, Raft 99).
- **The small-model winners (`kgram`/`hybrid`) do not carry to Raft** — both plateau at
  ~51 (a representational ceiling). And causal's Raft lead is **fragile/high-variance**:
  half the 12 seeds are stuck at the ~51 plateau, half break out (101–222).

## Takeaway

- **On these models, plain `random` is the most effective bug-finder — it beats every
  feedback variant, regardless of timeline representation, on all four bugs.** Feedback's
  exploitation appears to *hurt* at this scale (random's broad sampling wins). The
  earlier "causal is the best bug-finder" conclusion was an artifact of comparing only
  feedback variants to each other; it does not survive a `random` reference.
- **The timeline representation only re-orders feedback *internally*.** Where it matters
  (the hard liveness bug) **causal is the strongest feedback repr** and **kgram the
  weakest**; for safety bugs it is immaterial. So the representation work is a genuine
  improvement *to feedback*, not to bug-finding overall on these models.
- **Coverage is model-dependent with no global winner**: causal excels on larger/
  concurrent models and collapses on request/response ones; kgram/hybrid are the reverse.
- **`--timeline-payload` (whole-payload) is too fine and degenerates** — needs
  selective-field hashing.

**Honest caveats (independent audit):** one discriminating liveness bug; heavy censoring
at cap 10000 (found-rate is the primary metric; all-seed medians are the honest latency);
low power (random-vs-causal found-rate p=0.242 — cannot claim they differ in reliability);
tutorial + one larger (Raft) model, no production scale; the "early-phase / plateau"
mechanisms are inferred from the data, not confirmed in code.

**Recommendation.** Keep `pairwise` as the safe default. The pluggable-representation
feature is a sound, tested, flag-gated addition that improves feedback *internally*
(`causal` best for hard liveness bugs among feedback variants; `causal`/pairwise best for
coverage on larger models), but on these models **plain `random` remains the better
bug-finder** — so no default flip is warranted. The genuine open question is whether
feedback (and thus a richer representation) pays off on **production-scale** state spaces,
where random is expected to flail; that is the study still worth running. Harness:
`fest-eval/run.py e9` (parallel) reproduces every number here.

