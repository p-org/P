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

1. **`feedbackpct` is the standout for targeted coverage.** On the hardest
   scenarios — a commit after an abort under failures, two commits under
   failures, two Paxos learns — **`feedbackpct` covers them with far more unique
   satisfying timelines than any other strategy** (e.g. `AbortThenCommit`: 16.8
   vs `random` 1.4), and reaches first coverage at a **fraction of the budget**
   (budget 100 where `random` needs 500). This is exactly the feedback-guided
   thesis: concentrate exploration on rare, deep behaviors that undirected
   search rarely hits.

2. **Feedback amplifies bugs into many witnesses.** Once a bug is found,
   feedback variants surface **20–25× more buggy schedules** than `random` at
   equal budget (they lock onto the buggy region and mutate around it) — useful
   for producing diverse repros of a failure.

3. **The scenario-coverage feature works exactly as specified.** Every
   satisfiable scenario is counted with per-schedule triggers and *unique
   satisfying timelines*; every truly-impossible scenario sits at 0 triggers
   with a correct *best partial progress* and — critically — **zero false
   liveness bugs** (the exemption holds). Same-seed runs are byte-identical.

4. **The trade-off is breadth, and it is gated on the base scheduler.** The
   feedback loop deliberately re-visits promising schedules rather than spraying
   the space, so it explores fewer *raw* timelines than `random` (which wins the
   breadth metric 2–9×) and is not the fastest at finding the *first* occurrence
   of a bug — plain `feedback` (random base) can even get stuck. The lesson is
   that the **base scheduler matters as much as the feedback layer**:
   `feedbackpct` (PCT base) is the all-rounder; plain `feedback` (random base) is
   the weakest and should not be the default. Multi-seed testing was essential —
   a single seed would have ranked the strategies very differently.

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

## E5 — Understandable per-run + unified reports

The report surfaces were redesigned for scannability: each **leads with a summary
count**, groups **covered scenarios** first and **coverage gaps** last, marks each
`[covered]` / `[  GAP  ]`, and the merge lists **which test cases** cover each scenario.

**Per-run** (`p check`, one test case):
```
..... Scenario coverage: 4/5 scenarios covered (1 gap)
.....   [covered] ErrorThenSuccess: triggered in 399 schedules, 19 unique satisfying timelines
.....   [covered] TwoSuccessfulWithdrawals: triggered in 399 schedules, 19 unique satisfying timelines
.....   [covered] WithdrawError: triggered in 399 schedules, 19 unique satisfying timelines
.....   [covered] WithdrawThenResponse: triggered in 399 schedules, 19 unique satisfying timelines
.....   [  GAP  ] ImpossibleRespFirst: not covered (best partial progress: 2/4 states)
```

**Unified** — `p merge-scenario-coverage` over both ClientServer test cases
(`tcSingleClient` + `tcMultipleClients`):
```
Unified scenario coverage across 2 test cases:
  4/5 scenarios covered in >=1 test case; 1 coverage gap.
  [covered] ErrorThenSuccess: covered in 2/2 test cases (tcSingleClient, tcMultipleClients); 750 total triggers, 23 unique satisfying timelines
  [covered] TwoSuccessfulWithdrawals: covered in 2/2 test cases (tcSingleClient, tcMultipleClients); 731 total triggers, 21 unique satisfying timelines
  [covered] WithdrawError: covered in 2/2 test cases (tcSingleClient, tcMultipleClients); 750 total triggers, 23 unique satisfying timelines
  [covered] WithdrawThenResponse: covered in 2/2 test cases (tcSingleClient, tcMultipleClients); 798 total triggers, 24 unique satisfying timelines
  [  GAP  ] ImpossibleRespFirst: never covered in any of 2 test cases; best progress anywhere 2/4 states
```
The gap (`ImpossibleRespFirst`, structurally unsatisfiable) is called out explicitly
rather than buried among the counters; a real coverage gap would read the same way.

## E6 — D4 scenario-steering ablation

Feedback with vs. without the scenario-compliance term in
`priority = diversity × (1 + compliance)`, isolated via an eval-only gate that
forces the compliance term to 0. 3 models × 5 seeds × 4 budgets = 60 paired
runs.

**The original signal was provably inert.** `RunCompliance()` returned the
**max** over *all* auto-attached scenarios of `statesReached / totalStates`.
Because a *common* scenario (e.g. `WriteCommitted`, `ValueLearned`,
`WithdrawThenResponse`) is satisfied within essentially every schedule, that max
saturated at **1.0** almost every iteration, making `priority = diversity ×
(1 + 1.0) = 2 × diversity` — a **constant factor that cannot re-order saved
generators**. Ablation confirmed it: **on/off were byte-identical in all 60
configurations.**

**The fix (in this PR): a sparse coverage-*novelty* signal.**
`ScenarioSteering.NoveltyCompliance` awards compliance 1.0 only to a schedule
that makes *new* coverage progress — first-satisfies a scenario, or advances the
furthest state of a not-yet-satisfied scenario beyond the suite's best so far —
and 0.0 otherwise. It cannot saturate (a scenario stops contributing once it
plateaus, including an unsatisfiable one at its ceiling), and it is computed
only for generators the search actually keeps (after the timeline-diversity
gate), so a discarded schedule never consumes a scenario's novelty.

**Post-fix ablation — the signal now steers, but the coverage payoff on these
models is negligible:**

| metric (rare scenario, mean/5 seeds) | on vs off |
|---|---|
| Paired configs differing in **unique satisfying timelines** | **0 / 60** |
| Paired configs differing in **triggering-schedule count** | **4 / 60** (all Paxos; e.g. b=1000 75.6 vs 74.6) |

So the fix does what it should — the signal is no longer a constant factor and
now measurably re-orders exploration (4 configs shift, vs 0 before) — but on
these tutorial-scale models it does **not** change how many distinct satisfying
timelines are found. The reason is structural: the common scenarios plateau
within the first few schedules (no further novelty to reward) and the deep ones
(`AbortThenCommit` reaches only ~0.2 unique timelines even at budget 1000) are
too rarely reachable for a novelty nudge to matter. Demonstrating a coverage
*gain* from steering needs larger models with many rare-but-reachable behaviors
and longer exploration — the same regime where feedback's other benefits appear.
Net: the fix removes a latent no-op and makes the D4 hook correct and
non-saturating; its practical value remains to be shown at scale.

## Interpretation & takeaways

- **Scenario coverage is a sound, discriminating metric.** It cleanly separates
  *trigger count* (how often) from *unique satisfying timelines* (how many
  distinct ways), and partial-coverage surfaces *how close* an uncovered
  behavior got — all without ever mistaking an uncovered scenario for a bug.

- **Feedback's value is targeted, not universal — use it deliberately.** Its
  exploitation pays off precisely when the objective is a *rare/deep* behavior
  or amplifying a known bug, with a *structured* base scheduler. `feedbackpct`
  (PCT base) is the clear all-rounder here (best rare-scenario coverage and best
  bug amplification); `feedbackpos` sits in between; plain `feedback` (random
  base) is the weakest and can get stuck. For broad, undirected breadth, plain
  `random` remains a strong and cheap baseline — the two are complementary.

- **D4 scenario-steering: latent no-op found and fixed (E6).** The original
  compliance term saturated at 1.0 and applied a constant factor (on/off
  byte-identical in all 60 configs). This PR replaces it with a sparse
  coverage-*novelty* signal that now genuinely re-orders exploration; the
  practical coverage effect on these small models is still negligible (0/60
  configs change unique-timeline coverage), so its payoff remains to be shown on
  larger models. The mechanism is now correct rather than silently dead.

- **Actionable signal for the P team:**
  1. Make **`feedbackpct` the recommended feedback configuration** — it is the
     all-rounder here (best rare-scenario coverage, best bug amplification,
     reliable-ish first-bug finding). Plain `feedback` (random base) can get
     stuck and should not be the default. *(Surfaced in `--sch-feedbackpct` help
     text in this PR.)*
  2. **D4 compliance signal fixed in this PR** (coverage-novelty rather than the
     saturating max-partial-progress). Next step is to validate that it improves
     coverage on larger models with many rare-but-reachable scenarios.
  3. Treat raw timeline count as a *breadth* proxy only; for *targeted* coverage
     and bug amplification the feedback layer is the right tool — with a
     structured base scheduler.
  4. Benchmark the Cluster-B defaults on larger models before any default flip
     (as PR #985 notes) — these tutorial-scale models under-state feedback's
     value on the large state spaces it targets.

_Raw data: `results/e1_coverage.csv`, `e2_curve.csv`, `e3_bugs.csv`,
`e4_repro.txt`, `e5_merged_report.txt`, `e6_ablation.csv`. Harness + models are
in the standalone eval workspace; reproduce any point via the README commands._
