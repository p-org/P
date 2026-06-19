# Implementation Plan: Agentic Invariant Learning in PeasyAI

Companion to [`agentic-invariant-learning-design.md`](./agentic-invariant-learning-design.md).
This is the **iteration surface**: open design decisions to settle first, then a phased task plan.
Nothing here is committed to yet — it's meant to be edited and argued over.

---

## A. Open design decisions (settle these before coding)

Each is a fork that materially changes the build. Listed with options + a recommendation to react to.

### D1. Engine relationship — drive Specy, or replace its enumerator?
- **(a) Agent drives Specy** as the sound core: LLM does UG1/UG2/UG3 + ranking, Specy's grid
  search + Daikon + Z3/PChecker pruning does the learning. *Soundness for free; needs the
  `experimental/pinfer` engine.*
- **(b) Agent replaces the enumerator**: LLM proposes candidates directly; reuse only Specy's
  `TransformToP` + pruning + PChecker-falsify. *Works without the full engine; less sound,
  monitor-fidelity bottleneck.*
- **(c) Hybrid / both, selected at runtime** by engine availability and protocol size.
- **Recommendation:** **(c)** — build the agentic path first (works on `master` today), make the
  Specy path a pluggable backend behind the same service interface.
- **Decision:** _TBD_

### D2. What does the proposer emit?
- **(a) Specy grammar formulas** (`G→W∧H`), then `TransformToP` → monitor. *Inherits sound
  translation; needs the engine for TransformToP.*
- **(b) Hand-written P spec monitors.** *Works today via the agentic scripts; ~28% don't compile →
  repair loop required; vacuity risk.*
- **Recommendation:** prefer **(a)** when the engine is present; **(b)** as the fallback, always
  behind the repair loop.
- **Decision:** _TBD_

### D3. Trace-filter implementation (Stage A)
- **(a) New Python trace evaluator** over `*.trace.json` (own predicate evaluator).
- **(b) Reuse Specy's Daikon Dynamic-Learner-Interface evaluation** (the Java side already
  evaluates predicates over traces).
- **Recommendation:** start with **(a)** (small, no engine dependency, fast to ship Stage A);
  migrate to **(b)** once consolidated.
- **Decision:** _TBD_

### D4. PeasyAI tool surface
- **(a) One orchestrator tool** `peasy-ai-learn-invariants` (propose→validate→judge→rank).
- **(b) Several composable tools** (propose / validate / rank) + the orchestrator.
- Long-running concern: model-checking many candidates can take minutes — sync MCP call vs
  job/poll (`peasy-ai-learn-invariants` returns a handle; `peasy-ai-learn-status`).
- **Recommendation:** **(b)** composable + orchestrator; add async/poll only if Stage B latency
  proves it necessary.
- **Decision:** _TBD_

### D5. Soundness labeling
How do we present results? Proposal: three tiers — `proven` (PVerifier), `model-checked up to N
schedules` (PChecker), `holds-on-traces` (trace-filter only). Never call a bounded result "proven."
- **Decision:** _TBD_

### D6. Ranking signal
Ranking by "novelty vs developer specs" needs the developer's existing specs (`S_g`). Where from —
the project's `PSpec/`? A user-provided golden set? Pure interestingness heuristic when absent?
- **Decision:** _TBD_

### D7. Evaluation dataset
Reuse Specy's 43 goal specs (needs the `PInfer-OOPSLA-Artifact`) vs the P tutorials for fast
iteration. Probably both: tutorials for CI-speed loops, the 43 for the headline numbers.
- **Decision:** _TBD_

---

## B. Phased task plan

### Phase 0 — Consolidation (prerequisite, blocks the Specy-backend path)
- [ ] Decide the consolidation target: merge `experimental/pinfer` engine onto a feature line, or
      reference it as a submodule/build artifact. (Ties to the earlier merge-readiness analysis:
      tests, bundled jars, Amazon-Linux-only, portability.)
- [ ] Confirm `master` PChecker can emit the per-event JSON traces we need for Stage A (it can:
      `JsonWriter.cs`).
- **DoD:** one branch that builds `p`, runs PChecker with JSON traces, and (optionally) `p infer`.

### Phase 1 — Propose layer (works on `master` today; no engine needed)
- [ ] `Src/PeasyAI/src/core/services/learn_invariants.py`: `LearnInvariantsService(BaseService)`
      with `propose(project_path, design_doc, context_files, priors=['templates','lenses'])`.
- [ ] Reuse the template grammar from `Src/PInfer/Scripts/propose_templated.js` and the lens
      prompts; emit candidates (D2 decides formula vs monitor).
- [ ] `Src/PeasyAI/src/ui/mcp/tools/invariants.py`: `peasy-ai-propose-invariants` (+ register in
      `server.py`), `with_metadata`-wrapped preview response.
- **DoD:** `peasy-ai-propose-invariants` returns candidate specs for a tutorial project.

### Phase 2 — Validation / pruning layer (the core ask)
- [ ] **Stage A** `VacuityValidator` (trace-filter replay + guard-never-fires detection) in
      `validation/validators.py`; register in `CORE_VALIDATORS` after `SpecObservesConsistencyValidator`.
- [ ] **Stage C** vacuity canary (reuse the `<Name>_canary` scheme from `check_candidates.py`).
- [ ] **Stage B** `FalsePositiveValidator` — bounded `p check` via `CompilationService.run_checker`
      (behind a flag so the static chain stays fast).
- [ ] `InvariantLearningPipeline` shared by the MCP tool and a `LearnInvariantsStep` in
      `workflow/p_steps.py`.
- **DoD:** given candidates + traces, the pipeline removes vacuous + falsified and returns a verdict
      table; reproduces the session's FailureDetector result (vacuous "accuracy" monitor flagged).

### Phase 3 — Judge + rank + orchestrator
- [ ] LLM judge implementing Specy's Table 3 (verified / over-strong / bug / vacuous), grounded in
      the counterexample.
- [ ] `peasy-ai-rank-invariants` (D6 signal) + naming/explanation.
- [ ] `peasy-ai-learn-invariants` orchestrator (propose→validate→judge→rank), full response shape.
- **DoD:** end-to-end on a tutorial: design doc → ranked, classified, machine-checked spec set.

### Phase 4 — Specy-engine backend + guidance automation
- [ ] Pluggable Specy backend in `LearnInvariantsService` (drive `p infer` / hints when present).
- [ ] LLM UG1 (event combos) / UG2 (custom predicates) / UG3 (state exposure) automation feeding
      the engine.
- **DoD:** Specy backend selectable; LLM guidance raises the automatic-learning fraction.

### Phase 5 — Evaluation
- [ ] Harness over the eval dataset (D7); metrics: recall of goal specs, automation %, precision,
      vacuity rate, false-positive rate, novel-spec discovery, cost.
- [ ] Ablations: templates-only / lenses-only / both; trace-filter-only / +PChecker; ±guidance.
- **DoD:** a results table comparable to the paper (43/43, 65% automatic) with our deltas.

---

## C. Prerequisites & dependencies
- PChecker JSON traces (✅ `master`).
- Agentic scripts (✅ this branch / `Src/PInfer/Scripts/`).
- Specy engine for Phases 4–5's sound path (⚠️ `experimental/pinfer` only — Phase 0).
- PeasyAI MCP/service/validation framework (✅ `master`).
- LLM provider config (PeasyAI `~/.peasyai/settings.json`).

## D. Immediate next actions (proposed)
1. Settle **D1, D2** (the two big forks) — everything else follows.
2. Stand up **Phase 1** (propose layer) on `master`, since it needs no engine and validates the
   tool/service wiring early.
3. Prototype **Stage A** trace-filter on one tutorial to de-risk the cheap-vacuity claim.

---

*This plan is intended to be iterated. Edit decisions in §A, re-scope phases in §B, and we converge
before writing production code.*
