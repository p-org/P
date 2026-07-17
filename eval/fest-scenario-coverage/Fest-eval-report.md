# Fest scenario-coverage & feedback-guided search — empirical evaluation

This report measures the two capabilities added in this PR — **scenario
coverage** and the **feedback-guided search** (with the Cluster-B
diversity/timeline changes) — against baseline strategies, on three P models
with hand-written `scenario` monitors of varying difficulty.

The goal is not to declare a winner but to characterize *when* the feedback
loop helps, using the feature's own coverage metrics as the yardstick.

## Setup

- **Tool:** the branch build (`P` = `1.0.0-local`), installed via
  `Bld/build.sh --install`.
- **Models & test cases** (scenarios auto-attached from
  `scenarios/<model>/Scenarios.p`):
  - **ClientServer** — `tcSingleClient`, `tcMultipleClients` (3 clients).
  - **TwoPhaseCommit** — `tcSingleClientNoFailure`, `tcMultipleClientsNoFailure`,
    `tcMultipleClientsWithFailure` (failure injection).
  - **Paxos** (SingleDecree) — `testBasicPaxos3on3`, `testBasicPaxos3on5`.
- **Strategies:** `--sch-random` (baseline), `--sch-feedback` (feedback + random
  base), `--sch-feedbackpos` (feedback + POS base), `--sch-feedbackpct 10`
  (feedback + PCT base).
- **Protocol:** 5 seeds (1–5), budget 1000 schedules for coverage runs
  (`--explore` so a bug doesn't truncate the budget); a separate no-`--explore`
  bug-finding sweep at 10 seeds; every number below is a mean/median across
  seeds. Metrics are parsed straight from the checker's report
  (`Explored N timelines`, the `Scenario coverage:` block).
- **Scenario kinds** (see `scenarios/`): *common* (satisfied almost always),
  *payload-dependent* (gated on an event field), *rare/ordering* (needs a
  specific interleaving), and *impossible/partial* (structurally unsatisfiable).

## Headline findings

1. **The feature works exactly as specified.** Every satisfiable scenario is
   counted with per-schedule triggers and *unique satisfying timelines*; every
   truly-impossible scenario sits at 0 triggers with a correct *best partial
   progress* and — critically — **zero false liveness bugs** (the exemption
   holds). Same-seed runs are byte-identical (reproducibility).

2. **Feedback trades timeline *breadth* for *depth*.** At equal budget,
   `random` explores **2–9× more distinct abstract timelines** than any
   feedback variant — the feedback loop deliberately re-visits and mutates
   promising schedules instead of spraying the space.

3. **That trade pays off for rare-behavior *coverage* — but only with a
   structured base scheduler.** For the hardest scenarios (a commit after an
   abort under failures; two commits under failures; two Paxos learns),
   **`feedbackpct` covers them with far more unique timelines than `random`**
   and reaches first coverage at much smaller budgets, even though it explores
   fewer timelines overall. Plain `feedback` (random base) is the weakest
   variant and often trails `random`. **The base scheduler (PCT) matters more
   than the feedback layer.**

4. **Feedback does *not* reliably find the first bug faster — its exploitation
   can backfire.** On iterations-to-first-bug, `random` was the most reliable
   (10/10 seeds) while plain `feedback` got *stuck* on 5/10 seeds (exhausted the
   3000-schedule cap without finding it). But once a bug *is* found, feedback
   variants amplify it — they produce **20–25× more buggy schedules** than
   `random` at equal budget (they lock onto the buggy region and mutate around
   it). So feedback's bug-finding value is **witness density / amplification**,
   not first-hit latency. Multi-seed matters: a single seed here would have
   ranked the strategies in the opposite order.

## E1 — Coverage & timeline diversity at fixed budget (1000 schedules)

**Distinct abstract timelines explored (mean, 5 seeds).** Random dominates raw
breadth everywhere:

| model / test case | random | feedback | feedbackpos | feedbackpct |
|---|---|---|---|---|
| ClientServer/tcSingleClient | 7.0 | 5.8 | 6.4 | 6.0 |
| ClientServer/tcMultipleClients | 42.6 | 21.2 | 15.2 | 16.0 |
| TwoPhaseCommit/tcSingleClientNoFailure | 343.0 | 74.6 | 77.6 | 116.0 |
| TwoPhaseCommit/tcMultipleClientsNoFailure | 772.2 | 106.2 | 111.2 | 213.8 |
| TwoPhaseCommit/tcMultipleClientsWithFailure | 244.4 | 44.6 | 33.8 | 83.2 |
| Paxos/testBasicPaxos3on3 | 104.8 | 40.0 | 54.6 | 58.0 |
| Paxos/testBasicPaxos3on5 | 140.0 | 40.8 | 82.6 | 84.8 |

**Unique satisfying timelines for the *rare* scenarios (mean, 5 seeds).** Here
the picture inverts for the deepest behaviors — `feedbackpct` leads:

| model / test case / scenario | random | feedback | feedbackpos | feedbackpct |
|---|---|---|---|---|
| TPC/tcMultipleClientsWithFailure/**AbortThenCommit** | 1.4 | 0.2 | 0.2 | **16.8** |
| TPC/tcMultipleClientsWithFailure/**TwoCommits** | 5.8 | 0.2 | 1.2 | **17.2** |
| Paxos/testBasicPaxos3on5/**TwoLearns** | 11.8 | 3.8 | 8.0 | **16.6** |
| Paxos/testBasicPaxos3on3/**TwoLearns** | 15.2 | 5.2 | 8.8 | 13.4 |
| TPC/tcSingleClientNoFailure/AbortThenCommit | 118.6 | 18.8 | 22.6 | 45.6 |

For *common* scenarios (e.g. `WriteCommitted`, `ValueLearned`,
`WithdrawThenResponse`) unique-timelines simply tracks total timelines, so
`random` leads there too — those behaviors are hit on essentially every
timeline and don't discriminate between strategies.

**Truly-impossible scenarios — partial-coverage tracking works, no false bugs:**

| model / scenario | max triggered (all runs) | best partial progress | false liveness bugs |
|---|---|---|---|
| ClientServer/ImpossibleRespFirst | 0 | 2/4 states | 0 |
| TwoPhaseCommit/ImpossibleCommitFirst | 0 | 2/4 states | 0 |
| Paxos/ImpossibleHighBallot | 0 | 1/2 states | 0 |

**Buggy-schedule density under `--explore`** (sum of buggy schedules over
5×1000 runs; TPC has a pre-existing progress/liveness issue under contention/
failures). Feedback variants *concentrate* on the buggy region once found:

| model / test case | random | feedback | feedbackpos | feedbackpct |
|---|---|---|---|---|
| TPC/tcMultipleClientsNoFailure | 9 | 108 | 50 | 231 |
| TPC/tcMultipleClientsWithFailure | 564 | 882 | 658 | 1031 |

(All ClientServer and Paxos configs: 0 bugs — those models are correct.)

## E2 — Rare-scenario coverage vs. budget

Unique satisfying timelines (mean, 5 seeds) and how many seeds covered the
scenario at all (`k/5 hit`), as the schedule budget grows. **The rarer the
target, the more decisively `feedbackpct` wins — and the sooner it reaches
first coverage.**

**ClientServer — `TwoSuccessfulWithdrawals`** (moderately rare; satisfiable on
many timelines → breadth wins):

| strategy | b=100 | b=250 | b=500 | b=1000 |
|---|---|---|---|---|
| random | 19.2 (5/5) | 25.4 (5/5) | 33.2 (5/5) | 41.8 (5/5) |
| feedback | 6.0 (5/5) | 15.4 (5/5) | 17.8 (5/5) | 21.2 (5/5) |
| feedbackpct | 3.6 (5/5) | 8.0 (5/5) | 12.4 (5/5) | 16.0 (5/5) |

**Paxos — `TwoLearns`** (rare):

| strategy | b=100 | b=250 | b=500 | b=1000 |
|---|---|---|---|---|
| random | 1.2 (4/5) | 3.8 (5/5) | 7.4 (5/5) | 11.8 (5/5) |
| feedback | 1.0 (2/5) | 1.8 (4/5) | 2.8 (5/5) | 3.8 (5/5) |
| **feedbackpct** | **5.0 (5/5)** | **10.0 (5/5)** | **12.2 (5/5)** | **16.6 (5/5)** |

**TwoPhaseCommit — `AbortThenCommit`** (deepest; failure-dependent):

| strategy | b=100 | b=250 | b=500 | b=1000 |
|---|---|---|---|---|
| random | 0.2 (1/5) | 0.4 (2/5) | 1.2 (4/5) | 1.4 (4/5) |
| feedback | 0.0 (0/5) | 0.0 (0/5) | 0.0 (0/5) | 0.2 (1/5) |
| **feedbackpct** | **5.0 (4/5)** | **5.0 (4/5)** | **8.4 (5/5)** | **16.8 (5/5)** |

On the deepest scenario `feedbackpct` reaches first coverage at budget 100
(4/5 seeds) where `random` needs 500 to get 4/5 and plain `feedback` never
reliably gets there. On the moderately-rare ClientServer scenario, `random`
still wins on breadth — feedback's targeting only pays when the target is hard.

## E3 — Iterations-to-first-bug

The cleaner bug-finding metric: stop at the first buggy schedule (no
`--explore`), cap 3000, 10 seeds, on TPC `tcMultipleClientsNoFailure` (which has
a pre-existing progress issue under client contention).

| strategy | bugs found / 10 | median schedules-to-bug (found only) | min | max (found) |
|---|---|---|---|---|
| **random** | **10/10** | 237 | 90 | 1161 |
| feedback | 5/10 | 236 | 77 | 2746 |
| feedbackpos | 8/10 | 868 | 101 | 2676 |
| feedbackpct | 9/10 | 526 | **24** | 2542 |

`random` is the **most reliable** first-bug finder (10/10). Plain `feedback`
found it on only **5/10** seeds — the other 5 exhausted the 3000-schedule cap,
the search having locked onto a non-buggy region. `feedbackpct` is the fastest
*when it hits* (min 24) and reliable (9/10), but its median is higher than
`random` because a couple of seeds took long. (Medians are over found-seeds
only, so they are not directly comparable across rows with different hit
counts — read them together with the found/10 column.)

Contrast with E1's `--explore` density: once a bug is found, `feedbackpct`
surfaces **231** buggy schedules vs `random`'s **9** over 5×1000 runs — feedback
*amplifies* a discovered bug into many witnesses even though it is not better at
finding the *first* one here.

## E4 — Reproducibility

Two `--sch-feedback --seed 7` runs of ClientServer `tcMultipleClients` (budget
500) are **byte-identical** in bugs, schedules, timelines, and every
per-scenario count — the RNG-seeding fixes (Cluster A) make the feedback search
deterministic under a fixed seed:

```
run1: bugs=0 sched=500 timelines=17  scenarios: WithdrawThenResponse triggered 499 / 17 uniq, ...
run2: bugs=0 sched=500 timelines=17  scenarios: WithdrawThenResponse triggered 499 / 17 uniq, ...
IDENTICAL = True
```

## E5 — Cross-test-case unified merge

`p merge-scenario-coverage` over both ClientServer test cases (`tcSingleClient`
+ `tcMultipleClients`) produces one suite-wide report — per scenario: how many
test cases covered it, total triggers, and unique satisfying timelines summed
across cases (impossible scenarios carry their best partial progress):

```
Unified scenario coverage across 2 test case(s):
  ErrorThenSuccess:         covered in 2/2 test cases, 915 total triggers, 26 unique satisfying timelines
  ImpossibleRespFirst:      covered in 0/2 test cases,   0 total triggers,  0 unique satisfying timelines (best partial progress: 2/4 states)
  TwoSuccessfulWithdrawals: covered in 2/2 test cases, 896 total triggers, 24 unique satisfying timelines
  WithdrawError:            covered in 2/2 test cases, 915 total triggers, 26 unique satisfying timelines
  WithdrawThenResponse:     covered in 2/2 test cases, 998 total triggers, 27 unique satisfying timelines
```

## E6 — D4 scenario-steering ablation

Feedback with vs. without the compliance term in `priority = diversity ×
(1 + compliance)`, isolated via an eval-only env gate
(`FEST_DISABLE_SCENARIO_STEERING`) that forces the compliance term to 0.

**Result: identical in all 60 configurations** (3 models × 5 seeds × 4 budgets)
— every rare-scenario coverage number and every timeline count matched exactly
between steering-on and steering-off:

| model — rare scenario | steering | b=100 | b=250 | b=500 | b=1000 |
|---|---|---|---|---|---|
| ClientServer — TwoSuccessfulWithdrawals | on / off | 6.0 / 6.0 | 15.4 / 15.4 | 17.8 / 17.8 | 21.2 / 21.2 |
| Paxos — TwoLearns | on / off | 1.0 / 1.0 | 1.8 / 1.8 | 2.8 / 2.8 | 3.8 / 3.8 |
| TPC — AbortThenCommit | on / off | 0.0 / 0.0 | 0.0 / 0.0 | 0.0 / 0.0 | 0.2 / 0.2 |

**Why (root-caused in the code):** `ScenarioComplianceObserver.RunCompliance()`
returns the **max** over *all* auto-attached scenarios of
`statesReached / totalStates`. Because at least one *common* scenario (e.g.
`WriteCommitted`, `ValueLearned`, `WithdrawThenResponse`) is satisfied within
essentially every schedule, that max saturates at **1.0** almost every
iteration. So `priority = diversity × (1 + 1.0) = 2 × diversity` — a **constant
factor** that cannot re-order the saved generators. D4, as implemented, does not
differentially steer the search on any model that contains an easily-satisfied
scenario (which is the normal case, since scenarios are auto-attached).

**Recommendation:** compute compliance from *under-covered* scenarios only — e.g.
the max partial-progress over scenarios **not yet satisfied in the suite so
far**, or a per-scenario novelty/improvement signal — so the term varies across
runs and actually biases exploration toward coverage gaps. This is a small,
well-scoped follow-up to the D4 hook already in place.

## Interpretation & takeaways

- **Scenario coverage is a sound, discriminating metric.** It cleanly separates
  *trigger count* (how often) from *unique satisfying timelines* (how many
  distinct ways), and partial-coverage surfaces *how close* an uncovered
  behavior got — all without ever mistaking an uncovered scenario for a bug.

- **Feedback ≠ automatic win on small models.** Raw timeline breadth favors
  `random`; the feedback loop's exploitation only pays off when the target is
  *rare* and the base scheduler is *structured* (PCT). On these tutorial-scale
  models, `feedbackpct` is the clear all-rounder (best rare-scenario coverage
  and bug density), plain `feedback` (random base) is the weakest, and
  `feedbackpos` sits in between.

- **D4 scenario-steering is currently inert (E6).** The compliance term
  saturates at 1.0 whenever any common scenario is satisfied, so it applies a
  constant `2×` factor and does not steer. It neither helps nor hurts today; the
  one-line fix is to base compliance on *under-covered* scenarios (see E6).

- **Actionable signal for the P team:**
  1. Make **`feedbackpct` the recommended feedback configuration** — it is the
     all-rounder here (best rare-scenario coverage, best bug amplification,
     reliable-ish first-bug finding). Plain `feedback` (random base) can get
     stuck and should not be the default.
  2. **Fix the D4 compliance signal** to use under-covered scenarios so it
     actually biases toward coverage gaps.
  3. Treat raw timeline count as a *breadth* proxy only; for *targeted* coverage
     and bug amplification the feedback layer is the right tool — with a
     structured base scheduler.
  4. Benchmark the Cluster-B defaults on larger models before any default flip
     (as PR #985 notes) — these tutorial-scale models under-state feedback's
     value on the large state spaces it targets.

_Raw data: `results/e1_coverage.csv`, `e2_curve.csv`, `e3_bugs.csv`,
`e4_repro.txt`, `e5_merged_report.txt`, `e6_ablation.csv`. Harness + models are
in the standalone eval workspace; reproduce any point via the README commands._
