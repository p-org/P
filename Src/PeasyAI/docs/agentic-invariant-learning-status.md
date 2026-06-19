# Status: Agentic Invariant Learning (PInfer × PeasyAI)

Snapshot of the in-progress feature. Companion to
[`agentic-invariant-learning-design.md`](./agentic-invariant-learning-design.md) (the design) and
[`agentic-invariant-learning-plan.md`](./agentic-invariant-learning-plan.md) (the plan + open decisions).

**Branch:** `pinfer-peasyai-design` (pushed to `origin`, **no PR** — iteration branch).
**Delivery shape:** shared engine-agnostic core + two thin wrappers (PeasyAI MCP tool + Claude Code skill).

---

## TL;DR

The agentic invariant-learning workflow (**propose → validate → judge → rank**) is implemented as a
shared Python core with a PeasyAI MCP surface and a Claude Code skill. The **deterministic half**
(compile + bounded PChecker model-check + vacuity/false-positive pruning) is **built and verified
end-to-end**. The **LLM half** (propose/judge) is wired to PeasyAI's provider but **not yet run**
(no venv / LLM provider in the dev sandbox). Two architecture decisions (D1/D2) and the Specy-engine
backend remain open.

---

## Where everything lives

| Branch | Contents |
|---|---|
| `pinfer-agentic-invariants` (PR #974) | the original agentic toolkit: `check_candidates`, `prep_candidates`, `diag_compile`, `verify_repairs`, `repair_workflow.js`, README |
| **`pinfer-peasyai-design`** (this branch, no PR) | the design + plan docs, `propose_templated.js`, **and the implementation below** (built on top of the toolkit) |
| `experimental/pinfer` | the published **Specy/PInfer C# engine** (`Hint.cs`, `p infer`, `TransformToP`, grid search, Daikon, Z3) — needed for the sound-engine backend (Phase 4) |

---

## What's built (this branch)

### 1. Shared core — `Src/PInfer/Scripts/invariant_core.py`  ✅ verified
Engine-agnostic; no PeasyAI / Claude-Code dependency.
- `validate_candidates(...)` — wire specs → `p compile` → bounded `p check` → classify
  **HOLDS / FAILS(+cex) / VACUOUS / INCONCLUSIVE / COMPILE-ERR**, with `<Name>_canary` vacuity
  detection and pre-existing-SUT-failure → INCONCLUSIVE.
- Propose prompts (Specy template shapes + intent lenses), Specy-Table-3 judge rubric, P repair rules.
- `check_candidates.py` is now a thin CLI over it.

### 2. PeasyAI MCP wrapper  ✅ py_compile / ⚠️ runtime unrun
- `core/services/learn_invariants.py` — `LearnInvariantsService`: propose via `self.llm.complete`,
  validate via the shared core, judge failures, rank.
- `ui/mcp/tools/invariants.py` — `peasy-ai-learn-invariants`, `peasy-ai-validate-invariants`.
- Registered in `server.py` + `core/services/__init__.py`.

### 3. Claude Code skill  ✅
- `.claude/skills/learn-invariants/SKILL.md` — the multi-agent loop (fan-out proposers + adversarial
  judges) over the same scripts.

---

## Verified vs pending

| Item | Status |
|---|---|
| `py_compile` all new/changed Python | ✅ pass |
| Unit tests (`test_invariant_core.py`, 8) | ✅ pass |
| End-to-end validation on `Tutorial/4_FailureDetector` (compile → check → HOLDS) | ✅ pass |
| PeasyAI → `invariant_core` import path resolution | ✅ pass |
| MCP runtime path (LLM propose/judge) | ⚠️ not run — needs PeasyAI venv + LLM provider |
| Specy-engine backend (`p infer` / hints / `TransformToP`) | ⛔ not started (needs `experimental/pinfer`) |
| Cheap trace-filter Stage A (vacuity via trace replay) | ⛔ not started (today vacuity = PChecker canary) |
| Evaluation harness vs Specy's 43 goal specs | ⛔ not started |

---

## Known deviations / decisions outstanding

- **Validators not in `CORE_VALIDATORS`:** the design proposed `VacuityValidator` /
  `FalsePositiveValidator` in the validator chain, but `Validator.validate(code, context)` has no
  project/test handle to run `p check`, and it would slow the whole generation path. Validation lives
  in `LearnInvariantsService` instead. (Design doc §5 to be reconciled.)
- **Open decisions (see plan §A):** D1 (drive Specy vs replace its enumerator), D2 (emit Specy
  formulas + `TransformToP` vs hand-written monitors) are the two forks that gate Phase 4; D3–D7
  (trace-filter impl, tool surface, soundness labeling, ranking signal, eval dataset) follow.

---

## How to run

**Deterministic validation (works now):**
```bash
python3 Src/PInfer/Scripts/check_candidates.py --project Tutorial/4_FailureDetector \
  --candidates <candidates>.p --main TestMultipleClients \
  --assert-in "union { TestMultipleClients }, FailureDetector, FailureInjector" --iters 2000
python3 Src/PInfer/Scripts/test_invariant_core.py   # unit tests
```

**Full MCP path (after setup):**
```bash
cd Src/PeasyAI && python3 -m venv venv && source venv/bin/activate
pip install -r requirements.txt          # configure ~/.peasyai/settings.json (LLM provider)
# then call the peasy-ai-learn-invariants MCP tool
```

**Multi-agent (Claude Code):** invoke the `learn-invariants` skill on a P project.

---

## Suggested next steps

1. Settle **D1 + D2** (the two forks) — unblocks the Specy-engine backend.
2. Stand up the PeasyAI **venv + LLM provider** and do a live run on one tutorial to exercise the
   propose/judge path.
3. Prototype the cheap **trace-filter Stage A** (PObserve-style replay) for fast vacuity/false-on-data.
4. Build the **evaluation harness** (recall of goal specs, automation %, vacuity/false-positive rates)
   against the tutorials, then Specy's 43 goal specs.
