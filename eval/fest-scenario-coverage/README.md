# Fest scenario-coverage: examples + evaluation

This directory contains **example `scenario` monitors** and an **empirical
evaluation** of the Fest scenario-coverage + feedback-guided search added in
this PR. It is intentionally *not* part of any `.pproj`, so it is not compiled
by CI (`tutorials.yml`) — the `.p` files here are illustrative/reproduction
inputs, and the runnable copies live outside the repo (see below).

## What's here

- `scenarios/{ClientServer,TwoPhaseCommit,Paxos}/Scenarios.p` — documented
  example `scenario` monitors written against the corresponding `Tutorial/`
  models. They span the feature's dimensions: common, payload-dependent,
  rare/ordering, and impossible/partial (never-satisfiable) scenarios.
- `Fest-eval-report.md` — the measured results (coverage by strategy, timeline
  diversity, coverage-vs-budget, iterations-to-bug, reproducibility, partial /
  impossible tracking, cross-test-case merge, and the D4 steering ablation).

## What a `scenario` is (one paragraph)

A `scenario Name observes {..} { ..states.. }` lowers to a P **spec monitor**
with a coverage flag: it is **auto-attached to every test case** (no `assert`
needed), its accepting (**cold**) state means "this behavior was exercised",
and it is **exempt from the liveness check** (an unsatisfied scenario is
*uncovered*, not a bug). The checker reports, per test case, how many schedules
triggered each scenario, how many **unique satisfying timelines** were seen, and
— for scenarios never satisfied — the **best partial progress** (`X/Y states`).

## Reproducing the evaluation

```bash
# 1. Build + install the branch P tool (packs a nupkg, installs global tool)
./Bld/build.sh --install --skip-submodules -c Release
export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"

# 2. Copy a tutorial model somewhere writable and drop the scenarios into PSpec/
cp -r Tutorial/1_ClientServer /tmp/CS
cp eval/fest-scenario-coverage/scenarios/ClientServer/Scenarios.p /tmp/CS/PSpec/

# 3. Compile + run — the scenario-coverage block appears in the report.
#    --sch-feedbackpct is the recommended feedback strategy (see the report: best
#    rare-behavior coverage). Use --sch-random for a baseline comparison.
cd /tmp/CS && P compile
P check -tc tcMultipleClients --sch-feedbackpct 10 -s 1000 --seed 1 --explore

# 4. Merge per-test-case coverage into one unified report.
#    Each `p check` writes under <out>/<testCase>/<Mode>/, so different test cases are
#    kept separate (and re-runs rotate within their own folder). merge-scenario-coverage
#    recurses and keeps the LATEST artifact per test case, so re-runs never double-count.
P check -tc tcSingleClient    --sch-feedbackpct 10 -s 500 -o /tmp/CS/merge
P check -tc tcMultipleClients --sch-feedbackpct 10 -s 500 -o /tmp/CS/merge
P merge-scenario-coverage /tmp/CS/merge
```

The full multi-model / multi-strategy / multi-seed harness used for
`Fest-eval-report.md` is a standalone Python script (kept out of the repo);
the commands above reproduce any single data point.
