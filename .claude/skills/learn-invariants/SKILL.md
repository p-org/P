---
name: learn-invariants
description: Learn correctness specifications (invariants) for a P project via the agentic PInfer workflow — propose intent-level specs with template + intent-lens priors, model-check each with PChecker, prune vacuous specs and false positives, and judge survivors (verified / bug / spurious). Use when the user wants to mine, learn, or discover invariants/specifications for a P model, or asks "what properties should hold for this protocol".
---

# learn-invariants — agentic invariant learning for P

The multi-agent (Claude Code) wrapper of the PInfer agentic workflow. The deterministic
engine and all prompt text live in `Src/PInfer/Scripts/invariant_core.py`; the same engine
backs the PeasyAI `peasy-ai-learn-invariants` MCP tool. Prefer this skill when you want the
parallel, adversarial version (fan-out proposers + judges).

## Inputs to gather first
From the target P project (`<DIR>`):
- **events** — event names + payload types (read `<DIR>/PSrc/*.p`).
- **main** — the test's `[main=...]` machine, and **assert-in** — the module expression after
  `in` (both from `<DIR>/PTst/*.p`; copy verbatim).
- Optional **design doc / intent**.

## The loop

1. **PROPOSE (fan out).** Spawn proposer agents — one per template shape
   (`invariant_core.TEMPLATE_SHAPES`: arity-1, arity-2-same, arity-2-cross, exists) and one per
   intent lens (`INTENT_LENSES`: agreement, liveness, consistency, validity). Use
   `core.propose_system_prompt()` / `propose_user_prompt(...)`, or run the
   `Src/PInfer/Scripts/propose_templated.js` Workflow. Each returns JSON candidates
   (`name, intent, predictedBucket, observes, specCode`). Derive from **intent, not the
   implementation**.

2. **PREP.** Run `python3 Src/PInfer/Scripts/prep_candidates.py <proposer_result.json> <outdir>`
   for the deterministic P fixups (entity unescape, var-decl hoisting, `!in`/`for` rewrites).

3. **CHECK (the validation that removes vacuous + false positives).**
   `python3 Src/PInfer/Scripts/check_candidates.py --project <DIR> --candidates <cand>.p
   --main <MAIN> --assert-in "<MODULE_EXPR>" --iters 2000`
   → verdicts: HOLDS / FAILS(+cex) / VACUOUS / INCONCLUSIVE / COMPILE-ERR. Vacuity is caught via
   `<Name>_canary` probes; pre-existing SUT failures are flagged INCONCLUSIVE (not false bugs).

4. **REPAIR (recover compile failures).** For COMPILE-ERR candidates iterate
   `diag_compile.py` → `repair_workflow.js` (Workflow) → `verify_repairs.py`, feeding the exact
   compiler error back. Usually 2 rounds recovers all.

5. **JUDGE (fan out, adversarial).** For each FAILS, classify with `core.JUDGE_RUBRIC`
   (verified / bug / spurious / monitor-bug), grounded in the counterexample; for any judged a
   **bug**, run an adversarial refuter before confirming. A liveness failure "at end of program
   execution" is almost always spurious, not a bug.

6. **REPORT.** Rank verified-meaningful first; present the classified, machine-checked set with
   counterexamples for genuine bugs and rationale for spurious/over-strong.

## Notes
- Soundness label: results are "holds up to N schedules / on these traces", not proofs — escalate
  to PVerifier for proofs.
- Templates and lenses are complementary; run both and union the results.
- Scale fan-out to the ask: a few proposers for a quick pass; full template×lens grid + adversarial
  judges for an exhaustive sweep.
