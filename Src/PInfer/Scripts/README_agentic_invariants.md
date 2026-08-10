# Agentic invariant mining (propose → check → judge)

A lightweight, agent-driven alternative to brute-force trace mining. Instead of
enumerating predicates over traces, an LLM agent **proposes** the invariants a
protocol *should* satisfy by reading the P **source + design-intent comments**,
those candidates are **model-checked** by PChecker (the sound oracle), and the
agent then **judges** each result. PChecker — not the agent — is the source of truth.

```
  ┌─────────────────────────────┐   reads PSrc/*.p + design comments
  │ ① PROPOSE  (LLM agent)      │   emits intent-level `spec` monitors
  │   derive from INTENT, not    │   → a candidates .p file
  │   from the implementation    │
  └──────────────┬──────────────┘
                 ▼
  ┌─────────────────────────────┐   check_candidates.py:
  │ ② CHECK  (this tool)        │   wire monitors in, generate one test each,
  │   PChecker on EVERY candidate│   compile, model-check, classify,
  │   + vacuity canaries         │   extract counterexamples
  └──────────────┬──────────────┘
                 ▼  HOLDS / FAILS(+cex) / VACUOUS
  ┌─────────────────────────────┐   for each FAILS: required property → BUG,
  │ ③ JUDGE  (LLM agent)        │   else over-strong → SPURIOUS (repair/drop).
  │   grounded in the cex        │   for each HOLDS: meaningful vs vacuous.
  └─────────────────────────────┘
```

## Why this shape

* **Propose from intent, never from the implementation.** If candidates merely
  paraphrase the code, a bug becomes a "correct" invariant and is never found.
  Deriving from the protocol's *contract* lets PChecker measure the gap between
  intent and implementation — and that gap is where bugs live.
* **No trace pre-filter.** A failing candidate is *information*, not garbage: it
  is either a real bug (a required property the system violates) or an over-strong
  candidate. Only a sound check + judgment over the concrete counterexample can
  tell them apart, so every candidate goes to PChecker.
* **Vacuity is checked.** "HOLDS" can be a lie if the assertion's guard never
  fires. A `<Name>_canary` companion (`assert false` in the same guarded branch)
  reveals it: if the canary never trips, `<Name>` verified nothing.

## Using the checker

```bash
python3 Src/PInfer/Scripts/check_candidates.py \
  --project   Tutorial/4_FailureDetector \
  --candidates /path/to/candidates.p \
  --main      TestMultipleClients \
  --assert-in "union { TestMultipleClients }, FailureDetector, FailureInjector" \
  --iters     3000
```

* `--candidates` is a `.p` file containing one or more `spec <Name> observes ... { ... }`
  blocks (the proposer agent's output). A `spec <Name>_canary` block is treated as a
  vacuity probe for `<Name>`.
* `--assert-in` is the module expression that appears after `in` in the project's
  existing test (copy it from `PTst/TestScript.p`).
* The tool resolves `dotnet`/`DOTNET_ROOT` automatically and cleans up the files it
  generates (`PSpec/_candidates.p`, `PTst/_candidate_tests.p`) unless `--keep`.

Steps ① and ③ are LLM steps (run them with Claude Code / any agent); this script is
the deterministic middle. Validated on Two-Phase Commit, where the loop surfaced a
real read-your-writes consistency violation and correctly separated it from an
over-strong "all vote SUCCESS ⇒ commit" candidate that fails only due to benign timeouts.

## Compile-repair stage (recovers candidates that don't compile)

LLM-written spec monitors fail to compile ~28% of the time (concentrated in multi-state
liveness monitors): inline `var` init, `?:` ternary, `not`/`not in`, `{x}` string
interpolation, duplicate state names, seq-vs-set, invented event names. A two-script
loop recovers them by feeding the *exact compiler error* back to an LLM:

```bash
# 1. Diagnose: compile each candidate alone, capture its first compiler error.
python3 Src/PInfer/Scripts/diag_compile.py --out fails.json \
    FailureDetector=Tutorial/4_FailureDetector=fd_candidates.p
# 2. Repair (LLM): an agent per failing spec rewrites it given the error + P syntax rules.
#    repair_workflow.js is a Workflow; pass `fails.json` as its args; it returns fixed sources.
# 3. Verify: recompile each fixed spec.
python3 Src/PInfer/Scripts/verify_repairs.py repairs.json
```

Iterate steps 2-3: round 1 fixes the shallow errors; round 2 gets the deeper ones the
first fix exposes (e.g. an invented `eSpec_..._Init` event, a residual seq/set mismatch).
On the 4-benchmark sweep this recovered **all 23** previously-uncompilable candidates in
two rounds (15 then 8) — compile yield ~72% → ~100%. The repair agent must read the real
PSrc/PTst to avoid hallucinating event names, and is told to preserve the property, fixing
only syntax/encoding. Place repair AFTER deterministic `prep_candidates.py` (which already
unescapes entities, hoists `var` decls, and rewrites `!in`/`for`) so the LLM only handles
the genuinely diverse long tail.
```
