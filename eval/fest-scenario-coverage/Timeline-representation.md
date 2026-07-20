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

## Bake-off

Feedback strategy (`--sch-feedback`), 6 representations incl. two `--timeline-payload`
variants, on ClientServer / TwoPhaseCommit / Paxos. Numbers below are the
**larger-scale run** (20 seeds for coverage, 40 for the discriminating bug case),
which *corrected* a smaller 10-seed pilot — the pilot's eye-catching "causal 9/10"
bug-finding regressed to a marginally-significant 70% once more seeds were added, a
useful reminder to power these experiments. All claims were re-computed by an
independent audit of the raw CSVs.

### Coverage & distinct timelines (budget 1000, mean/20 seeds)

| model / test case | metric | pairwise | kgram | causal | hybrid | causal+P | hybrid+P |
|---|---|---|---|---|---|---|---|
| ClientServer/tcMultipleClients | distinct timelines | 21.8 | 37.4 | **1.4** | 47.9 | 51 | 51 |
| ClientServer/tcMultipleClients | rare uniq | 21.8 | 37.3 | **1.1** | 47.6 | 51 | 51 |
| TPC/tcMultipleClientsNoFailure | distinct timelines | 121.0 | 78.5 | **168.1** | 109.7 | 51 | 51 |
| TPC/tcMultipleClientsNoFailure | rare uniq (TwoCommits) | 119.3 | 77.6 | **165.8** | 108.2 | 50.4 | 50.4 |
| Paxos/testBasicPaxos3on5 | distinct timelines | 50.0 | 56.8 | 32.7 | 89.3 | 51 | 51 |
| Paxos/testBasicPaxos3on3 | distinct timelines | 41.0 | 53.5 | 60.3 | 102.3 | 51 | 51 |

- **`kgram`/`hybrid` improve coverage over `pairwise` in 4 of 5 configs** — but *not*
  universally: on **TPC/NoFailure `pairwise` (121) beats both `kgram` (78.5) and
  `hybrid` (110)**, where **`causal` is actually the top coverage method (168)**.
- **`causal` collapses to ~1.4 timelines on ClientServer** — the happens-before order
  over event *types* is schedule-invariant for request→response models, so it can't
  discriminate (the "too coarse" failure mode). It is *not* uniformly coarse: it's the
  best coverage method on TPC/NoFailure.
- **The `+payload` variants degenerate to a constant 51** (100/100 rows, and
  `causal+P` ≡ `hybrid+P` row-for-row — the base repr is entirely overridden). Payloads
  carry unique ids (`rId`, `transId`), so whole-payload hashing makes every delivery
  label unique → the diversity signal saturates at "always novel" — the **"too fine"**
  failure mode. This is a *payload-encoding* problem (needs selective-field hashing),
  not a flaw in payload-awareness per se.

### Iterations-to-first-bug (TPC `tcMultipleClientsNoFailure`, cap 3000, 40 seeds)

| repr | bugs found / 40 | found % | censored (hit cap) |
|---|---|---|---|
| **causal** | **28/40** | **70%** | 30% |
| hybrid | 20/40 | 50% | 50% |
| pairwise | 18/40 | 45% | 55% |
| kgram | 13/40 | 32% | 68% |
| hybrid+payload | 11/40 | 28% | 72% |
| causal+payload | 9/40 | 22% | 78% |

- **`causal` is the best bug-finder: 70% vs pairwise 45% (Fisher's exact p = 0.041).**
  Statistically significant but **marginal** — the 95% CIs overlap (causal
  [56, 84], pairwise [30, 60]); worth replicating on more seeds/bugs before calling
  it robust. Its advantage is that it is **censored less often** (30% vs 55% hit the
  cap), i.e. it finds *more* bugs — not that it finds them faster (among found runs its
  mean time is actually higher; heavier tail).
- **`kgram` *hurts* bug-finding (32%, below pairwise)** — finer local resolution helps
  coverage but dilutes the exploitation that reaches this bug. The `+payload` variants
  are worst (22–28%), the too-fine degeneracy again.
- `tcMultipleClientsWithFailure` (20 seeds) found the bug 20/20 for *every* repr — a
  dense, easy bug, non-discriminating (a control, not evidence).

## Takeaway

**The representation demonstrably matters, but there is no universal winner, and the
effect is workload-dependent — not a clean "coverage vs bug-finding" trade:**

- **`causal` is the best bug-finder here** (70% vs pairwise 45%, Fisher p = 0.041) —
  a *marginally* significant, replication-worthy result, and the payoff for wiring in
  the vector clocks the runtime already computes. It also had the **highest** coverage
  on TPC/NoFailure — yet **collapsed to ~1 timeline on ClientServer**. So causal isn't
  simply "trades coverage for bugs"; it's excellent where the happens-before shape
  varies across schedules and near-useless where it doesn't.
- **`kgram`/`hybrid` improve coverage in most configs but *hurt* bug-finding** (kgram
  32% < pairwise 45%): finer local resolution broadens exploration at the cost of the
  exploitation that reaches a specific bug.
- **`pairwise` (the default) is middling** — beaten by causal on bugs and by
  kgram/hybrid on coverage in most (not all) configs.
- **`--timeline-payload` (whole-payload hashing) is too fine and degenerates** the
  search (worst bug-finding, constant coverage). It needs *selective-field* hashing
  (status/ballot/term), not the raw payload with its unique ids.

**Honest caveats (from an independent audit of the raw CSVs):** this rests on **one
real bug on one discriminating test case**, 40 seeds, **heavy censoring** at cap 3000
(observed rates are lower bounds; the ranking could shift with a larger budget), and
**tutorial-scale models only** — no production-scale evidence. The causal significance
is marginal (overlapping CIs). Treat these as *promising, directional* results.

**Recommendation.** Keep `pairwise` as the safe default (zero behavior change) and
expose the knobs: `causal` for hunting bugs on protocol models (where its happens-before
signal varies), `kgram`/`hybrid` for coverage/diversity, and avoid whole-payload
`--timeline-payload` until it hashes selected fields. Any default flip should wait for
a larger, multi-bug, multi-scale study. The harness (`fest-eval/run.py e8`, parallel)
reproduces every number here.

