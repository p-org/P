# Design Report: Agentic Invariant Learning in PeasyAI

**Combining Specy/PInfer templates + agentic intent-lenses, with trace-filter and PChecker
quick-validation to prune vacuous specs and false positives — delivered as a PInfer feature in PeasyAI.**

Status: design proposal. Audience: P / PInfer / PeasyAI maintainers.

---

## 1. Goal

Let a P user point PeasyAI at a model (or a design doc + traces) and get back a ranked set of
**machine-checked correctness specifications** (safety + some liveness, `∀` and `∀∃`), with
vacuous and false-positive candidates already removed. This fuses three things that exist today
but are not connected:

1. **Specy/PInfer** — the published OOPSLA'26 engine (sound grammar-grid enumeration + Daikon +
   Z3/PChecker pruning). Lives on branch `experimental/pinfer`.
2. **Agentic inference** — LLM proposes intent-level specs, PChecker verifies, an LLM judges
   (`Src/PInfer/Scripts/`, this branch / PR).
3. **PeasyAI** — the AI-assisted P development surface (MCP tools → services → validation pipeline).

The thesis, validated empirically this session: **templates and lenses are complementary
proposer priors, and a cheap trace-filter followed by bounded PChecker falsification is enough
to strip the junk** — so an LLM "guidance + ranking" layer on top of Specy's sound core is the
high-value combination.

---

## 2. Background: the three ingredients

### 2.1 Specy / PInfer (the sound engine — `experimental/pinfer`)
Learns event-based specs of the form
```
φ : (∀ē_g)+ . G(ē_g)  →  (∃ē_w)* . W(ē_g,ē_w) ∧ H(ē_g,ē_w)        (Guard → Witness ∧ Hypothesis)
```
via: static **event-combination** selection from causal patterns (same-sourced-sends → ∀∀;
receive-then-send → ∀∃; send-then-listen → liveness); an **enumerative grid search** over
`(g,w,h)` (#preds in G, #preds in W, #terms in H's relate-set; default 2,2,2); **Daikon** to
dynamically learn H over filtered traces; **pruning** (syntactic subsumption, Z3 semantic,
symmetry → ~14.8× reduction); then **PChecker falsification** on fresh traces (only ~9% dropped).
Each surviving spec is auto-translated to a P runtime monitor. Evaluated on 14 protocols (incl.
Raft, Vertical Paxos, 3 AWS systems); **all 43 goal specs learned, 65% fully automatic**, the
rest needing user guidance UG1 (event combos) / UG2 (custom predicates) / UG3 (expose state).

### 2.2 Agentic inference (this branch — `Src/PInfer/Scripts/`)
A `propose → check → judge` loop:
- **propose** — an LLM derives intent-level specs from the source + design comments. Two priors:
  *intent-lenses* (agreement / liveness / consistency / validity) and *templates*
  (`propose_templated.js`: PInfer's `G→W∧H` shapes — arity-1 local, arity-2 same/cross relational, ∀∃).
- **check** — `check_candidates.py` wires each candidate spec into a P project, `p compile`s,
  `p check`s, and classifies `HOLDS / FAILS(+cex) / VACUOUS / INCONCLUSIVE`; `prep_candidates.py`
  + `repair_workflow.js` recover non-compiling candidates from the compiler error.
- **judge** — an LLM classifies each result as **verified / vacuous / bug / spurious**, grounded
  in the counterexample.

### 2.3 What we learned this session (the design drivers)
- **Templates ⊕ lenses > either.** On FailureDetector, templates uniquely found quantitative
  domain bounds (`trial ∈ [0,3)`); lenses uniquely found stateful semantics (down-set
  monotonicity). Use both.
- **The agent's self-prediction is unreliable** (12/15 model-checked failures were predicted
  "verified"). The oracle is non-negotiable.
- **Vacuity is a real failure of HOLDS.** A templated "accuracy" monitor that built its violation
  set but forgot to `assert` "held" at any depth — masking a known issue. → vacuity check must be
  mandatory on every HOLDS.
- **Failure ≠ bug.** Adjudicating 15 failures gave **0 bugs, 6 monitor-bugs, 9 spurious**; a naive
  "fails = bug" would have filed 15 false reports. This *is* Specy's Table 3 (golden vs learned:
  `S_g ⊂ S_l` = missing specs; `S_l ⇒ S_g` = over-strong from limited traces; "other" = real bug).
- **Monitor-encoding fidelity is the bottleneck** (28% of LLM specs didn't even compile; some that
  did were vacuous). Specy's sound construction avoids this; the agent needs guardrails.

> **Branch reality (important):** the Specy C# engine (`Hint.cs`, `p infer`, `TransformToP`,
> `PInferDriver`, grid search, Daikon interface, Z3 pruning) is **only on `experimental/pinfer`**.
> `master` has PChecker's per-event **JSON trace logging** (`JsonWriter.cs`, payloads + vector
> clocks), the spec-monitor language, PeasyAI, and the agentic scripts. **Prerequisite step 0 of
> this feature is consolidating the Specy engine + PeasyAI onto one buildable line.**

---

## 3. The combined model: "guided agentic inference"

```
            ┌─────────────────────── PROPOSE (LLM, dual priors) ───────────────────────┐
 design  ─▶ │  templates:  Specy G→W∧H shapes + causal event-combinations (UG1 auto)   │
 + code     │  lenses:     agreement / liveness / consistency / validity (semantic)     │ ─▶ candidate specs
 + traces   │  + custom predicates (UG2 auto) + state-exposure hints (UG3 auto)         │
            └──────────────────────────────────────────────────────────────────────────┘
                                              │
            ┌──────────── VALIDATE / PRUNE (cheap → sound) ────────────┐
 traces  ─▶ │ A. trace-filter replay  (ms)  → drop vacuous + false-on-data │
            │ B. bounded PChecker     (s)   → falsify remaining false-positives (sound) │ ─▶ survivors
            │ C. vacuity canary       (s)   → flag HOLDS whose guard never fires        │
            └──────────────────────────────────────────────────────────────────────────┘
                                              │
            ┌──────────────── JUDGE + RANK (LLM, = Specy Table 3) ─────────────────┐
            │ verified-keep · over-strong→relax · bug→report(cex) · vacuous→discard │ ─▶ ranked spec set
            └──────────────────────────────────────────────────────────────────────┘
```

**Why this shape.** Step 1 is where templates+lenses live (the proposer priors). Step 2 is the
user's explicit ask — strip vacuous/false **cheaply first, soundly second**. Step 3 is the
semantic interpretation Specy assigns to a human via Table 3, here automated by an LLM but grounded
in a concrete counterexample (which makes the judgment reliable, unlike a bare prediction).

---

## 4. Validation & pruning (the explicit requirement)

Two tiers, cheapest first, so PChecker only runs on candidates that already survived the data.

### Stage A — Trace-filter quick validation (cheap, no model checking)
Replay each candidate over the **already-collected** trace corpus (PChecker `*.trace.json`:
per-event `type/details{event,payload,clock,...}`), à la Daikon/Specy's dynamic evaluation:
- **Vacuity (cheap form):** if the candidate's Guard `G` has **no satisfying instantiation** in any
  trace (its quantified event tuples never occur, or `G` is never true), the spec is *vacuously*
  true → **discard / flag**. This catches the "guard never fires" class without model checking.
- **False-on-data:** if `H` is false for some instantiation where `G` holds, the candidate is
  false on the observed (correct) behavior → either junk **or** a real bug/over-strong spec; route
  to JUDGE with the offending trace as evidence (do **not** silently drop — that trace is the cex).
- Cost: milliseconds per candidate; kills the bulk before any `p check`.

### Stage B — Bounded PChecker validation (sound, removes false positives)
For Stage-A survivors: translate to a P monitor (Specy's `TransformToP`, or the agentic harness)
and run **bounded** `p check` (`-i N`, e.g. 1–3k schedules) via `CompilationService.run_checker`.
A candidate that fails here is **falsified** on an interleaving not present in the trace corpus →
remove (this is exactly Specy's PChecker-falsification pruning, which dropped ~9% with zero goal
specs lost). Survivors are "holds up to N schedules" — report the bound honestly.

### Stage C — Vacuity canary (sound complement to Stage A)
For each `HOLDS`, run the `<Name>_canary` mutant (`assert false` in the same guarded branch). If
the canary also passes (0 bugs ⇒ the branch is unreachable under model checking), the original is
**VACUOUS**. This catches monitor-encoding vacuity (the "forgot to assert" class) that trace-filter
alone can miss.

**Reuse, don't reinvent.** Specy already implements syntactic + Z3-semantic + symmetry pruning and
PChecker-falsification. The PeasyAI feature should **call Specy's pruning** when the engine is
available, and fall back to the agentic `check_candidates.py` (Stages B+C) + a new trace-filter
(Stage A) when it is not.

---

## 5. PeasyAI feature design (grounded in actual extension points)

PeasyAI pattern (verified): **MCP tool → core service → validation pipeline → metadata-wrapped
response**. Tools are `@mcp.tool`-decorated functions in `Src/PeasyAI/src/ui/mcp/tools/*.py`,
registered via `register_*_tools(mcp, get_services, with_metadata)` and wired in
`server.py`; they return `Dict[str,Any]` wrapped by `with_metadata(...)`. Services are singletons
from `get_services()`; `CompilationService.compile()` / `run_checker()` already shell out to
`p compile` / PChecker.

### 5.1 New MCP tools (`Src/PeasyAI/src/ui/mcp/tools/invariants.py`)
Follow `rag_tools.py`/`query.py`; register in `server.py` (`from ui.mcp.tools.invariants import
register_invariant_tools`; call it next to the others). Tools:

| Tool | Does |
|---|---|
| `peasy-ai-learn-invariants` | orchestrator: propose → validate → judge → rank; returns ranked specs + suggestions |
| `peasy-ai-propose-invariants` | propose-only (templates+lenses), returns candidate monitors for preview |
| `peasy-ai-validate-invariants` | run Stages A/B/C on a candidate set, return verdict table |
| `peasy-ai-rank-invariants` | rank a verified set by interestingness/novelty vs developer specs |

Response shape (per the contract): `with_metadata('peasy-ai-learn-invariants', { success, invariants:
[{formula, monitor_code, verdict, confidence, cex?, rationale}], suggestions: {event_combinations,
custom_predicates, expose_state}, summary: {proposed, verified, vacuous, false_positive, bugs},
message }, token_usage=...)`. Validate inputs first (`validate_project_path`, `check_input_size`).

### 5.2 New core service (`Src/PeasyAI/src/core/services/learn_invariants.py`)
`class LearnInvariantsService(BaseService)` with:
- `propose(project_path, design_doc, context_files, priors=['templates','lenses']) -> [Candidate]`
  — LLM proposer; reuses the `propose_templated.js` template grammar + the lens prompts.
- `learn(project_path, traces_dir=None, hint=None) -> LearningResult` — full orchestration; if the
  Specy engine is present, drives it (`p infer` / hint); else drives the agentic harness.
Add to `services/__init__.py` and `get_services()`. Returns a `LearningResult` (extend the
`ServiceResult` dataclass with `invariants`, `suggestions`, `token_usage`).

### 5.3 New validators (`Src/PeasyAI/src/core/validation/validators.py`)
Two `Validator` subclasses, registered in `CORE_VALIDATORS` **after**
`SpecObservesConsistencyValidator`:
- `VacuityValidator` — Stage A trace-filter + Stage C canary; severity WARNING (vacuous HOLDS).
- `FalsePositiveValidator` — Stage B bounded `p check` via `CompilationService.run_checker`;
  severity ERROR (falsified). (These call out to compilation; keep them behind a flag so the pure
  static validator chain stays fast for the existing generation path.)

### 5.4 Pipeline & data flow
A dedicated `InvariantLearningPipeline` (mirroring `ValidationPipeline`) invoked from both the MCP
tool and a new workflow step (`workflow/p_steps.py: LearnInvariantsStep`), so the MCP and workflow
paths share one implementation (the existing gotcha: keep logic in the pipeline class, not the
wrappers).

```
peasy-ai-learn-invariants
  └─ LearnInvariantsService.learn()
       ├─ propose (LLM: templates + lenses + UG1/2/3 suggestions)
       ├─ [Specy engine present?] ── yes ─▶ p infer (grid search + Daikon + Z3 prune + PChecker-falsify)
       │                            └ no  ─▶ agentic harness (prep → check_candidates → repair)
       ├─ InvariantLearningPipeline: VacuityValidator (A+C) → FalsePositiveValidator (B)
       └─ judge + rank (LLM, Table 3) ─▶ with_metadata(payload)
```

### 5.5 Reuse map
- **Trace generation:** `CompilationService.run_checker` already drives PChecker; emit JSON traces
  for Stage A.
- **Compile/check oracle:** `CompilationService.compile` / `run_checker` (Stage B), or the agentic
  `check_candidates.py`.
- **Monitor translation:** Specy `TransformToP` when present; else the agentic monitor wiring.
- **Repair:** `diag_compile.py → repair_workflow.js → verify_repairs.py` for non-compiling LLM specs.

---

## 6. The LLM as Specy's "guidance + ranking" layer

Specy's 35% non-automatic cases are exactly the LLM's sweet spot. Automate the user guidance:
- **UG1 (event combinations):** LLM proposes interesting combinations the causal heuristics miss
  (e.g., Chain Replication's read-write response pair) → feed as Specy config inputs.
- **UG2 (custom predicates):** LLM writes the domain predicates Daikon can't (Raft `⊑`, Chain Rep
  `⪯`) as P functions for the grammar.
- **UG3 (expose state):** LLM suggests which local state to surface as event payloads.
- **Ranking (Specy's stated future work):** LLM ranks the <100 pruned formulas by
  novelty-vs-developer-specs and importance, and **names/explains** each.

This is the additive hybrid: **Specy's sound enumerate-and-check core + LLM semantic guidance &
ranking** — it should *raise* the 65%-automatic figure without sacrificing soundness.

---

## 7. Evaluation plan

Benchmark against the paper's own yardstick.
- **Dataset:** the 43 goal specs across the 14 Specy protocols (+ the P tutorials for fast iteration).
- **Metrics:**
  - *Recall* — fraction of goal specs learned (target: match Specy's 43/43).
  - *Automation* — fraction learned with **no** user guidance (Specy: 65%; target: **raise** it via LLM UG1/2/3).
  - *Precision / vacuity rate / false-positive rate* — fraction of reported specs that are
    verified-meaningful vs vacuous vs falsified (our session: lenses+templates produced ~28%
    compile-loss and several vacuous HOLDS pre-validation; measure post-validation).
  - *Novel-spec discovery* — does it rediscover the MVCC-2PC specs developers overlooked?
  - *Cost / latency* — LLM tokens + wall-clock vs Specy's minutes.
- **Ablations:** templates-only vs lenses-only vs both; trace-filter-only vs +PChecker; with vs
  without LLM guidance layer.

---

## 8. Phased milestones

0. **Consolidate** the Specy engine (`experimental/pinfer`) + PeasyAI + agentic scripts onto one
   buildable branch. (Prerequisite.)
1. **Propose layer in PeasyAI** — `peasy-ai-propose-invariants` + `LearnInvariantsService.propose`
   (templates + lenses). Output: candidate monitors. *(small)*
2. **Validation layer** — `VacuityValidator` (A+C) + `FalsePositiveValidator` (B) + trace-filter.
   Wire into an `InvariantLearningPipeline`. *(medium)*
3. **Judge + rank** — Table-3 classification + ranking; full `peasy-ai-learn-invariants`
   orchestrator. *(medium)*
4. **Specy engine integration** — drive `p infer`/hints when present; LLM UG1/2/3 guidance
   automation. *(large)*
5. **Evaluation** against the 43 goal specs; tune. *(medium)*

---

## 9. Risks & open questions

- **Cross-branch consolidation (highest risk):** the sound engine and the integration target are on
  different branches; merging Specy's C# + Java + Z3/Daikon deps cleanly is non-trivial (see the
  earlier merge-readiness analysis: tests, bundled jars, Amazon-Linux-only, portability).
- **Monitor-encoding fidelity:** the LLM proposer's specs are unsound until checked and ~28% don't
  compile; the repair loop + Specy's `TransformToP` (sound by construction) mitigate, but the
  proposer should prefer emitting **Specy grammar formulas** (then `TransformToP`) over hand-writing
  monitors, to inherit soundness.
- **Trace quality:** Stage A is only as good as the corpus; thin traces → vacuous/over-strong specs
  (Specy's own limitation). Couple with PChecker trace generation across multiple configs.
- **Soundness boundary:** be explicit that agentic-proposed + bounded-PChecker results are "holds up
  to N schedules / on these traces," not proofs — escalate to PVerifier (event-based) for proofs,
  as Specy does for inductive invariants.
- **Determinism / reproducibility** of the LLM steps for a research tool — pin models, log prompts.

---

*Appendix — provenance:* this design is grounded in the Specy paper (OOPSLA'26, DOI
10.1145/3798209), the PInfer engine on `experimental/pinfer`, the agentic scripts in
`Src/PInfer/Scripts/`, and a mapping of PeasyAI extension points (MCP tools, `GenerationService`,
`ValidationPipeline`, `CompilationService`, workflow steps).
