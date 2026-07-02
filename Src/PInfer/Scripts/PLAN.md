# Unified P Specification Inference — PInfer core + agentic LLM layer

> **Status:** Design plan v2 (incorporates multi-agent review, 2026-06-30).
> **Scope decision:** Agentic-first, PInfer optional (plug-in). First milestone = Phases 0–4
> (inference + judge). Downstream (verifier / monitors / test-gap report) and cross-feeding are
> deferred to Phases 5–6.
> **Change log:** v2 corrects the portability contract, freezes a structured candidate schema,
> makes vacuity/attribution/validation backbone responsibilities, adds a reproducibility +
> metrics methodology, splits Phase 2, and reconciles §9 acceptance with the milestone scope.
> See §13 for the v1→v2 finding map.

---

## 1. Motivation

Formulating correctness specifications for industrial P models is a large, manual burden.
Two efforts attack this from opposite ends:

- **PInfer / Specy** (research system, `experimental/pinfer` branch; OOPSLA'26 paper "Specy:
  Learning Specifications for Distributed Systems from Event Traces") learns quantified,
  **event-based** specifications from **execution traces** by statically enumerating a
  grammar-bounded formula template, dynamically learning payload relations with Daikon, and
  pruning with Z3 + PChecker. It is **sound within its bounds** but **mirrors the
  implementation** — it rarely surfaces bugs, and 15/43 of its benchmark specs required manual
  user guidance.

- **The agentic layer** (`pinfer-agentic-invariants` = PR #974, and `pinfer-peasyai-design`)
  proposes specs with an LLM from **design intent + source**, has **PChecker verify every one**
  (the sound oracle), and an LLM **judges** each result over the concrete counterexample. It
  **can surface bugs** (the intent↔implementation gap) but is **unsound** — every candidate must
  be checked.

## 2. Core insight (a hypothesis this milestone begins to test, not an assumed fact)

PInfer and the agentic approach are **two proposers over overlapping fragments of the same
Specy grammar, feeding the same sound oracle (PChecker)**. Everything downstream of proposal —
prep, repair, validate, prune, rank, judge — can be shared. So the architecture is a **single
backbone with pluggable proposers**, not two parallel pipelines.

> **Caveat (was overclaimed in v1):** the two proposers do **not** emit identical grammars.
> PInfer emits grammar-conformant formulas *by construction*, including a first-class
> trace-order term `index()` (happens-before) and augmented-existential/quorum (SC) constraints.
> The portable LLM proposers emit free-form P monitors over a **strictly smaller** predicate
> fragment (`{==,!=,<,<=,>,>=,in}`, no `index()` unless explicitly added). We therefore say
> "overlapping fragments," and Phase 1 adds an optional grammar-conformance check (§6.6). The
> complementarity claim (PInfer high-recall / agentic bug-finding) is **asserted but not
> validated by this milestone** — see §8 for how we begin to measure it and §11 R7 for the risk.

Their strengths are complementary:

| | PInfer (enumerative + Daikon) | Agentic (LLM) |
|---|---|---|
| Derives from | implementation **traces** | **intent** (source + design docs) |
| Soundness | sound within grammar bounds; Z3 + PChecker pruned | unsound; every candidate checked |
| Finds bugs? | rarely (mirrors implementation) | **can** (intent↔impl gap) |
| Coverage | exhaustive but grid-bounded | creative but spotty |
| Event-combo selection | **AST analysis of send/recv/goto** (Specy §4.1) — primary recall driver | LLM guess over event list (no reachability analysis) |
| Relation vocabulary for H | **Daikon** (data-driven numeric relations) | LLM-guessed over 7 comparison ops |
| Ordering / happens-before | first-class `index()` term | not expressible in portable fragment |
| Quorum / cardinality (SC) | `config_event` + existential-n | not expressible in portable fragment |
| Raw output | thousands of formulas → needs ranking | few, named, explained candidates |
| Missing | naming, rationale, bug-hunting | recall of true invariants, sound pruning, `index()`, SC, Daikon numerics |

**Cross-feeding is the north star** (Phase 6, deferred):
1. Agentic candidates get PInfer's **sound Z3 pruning**, not just PChecker falsification.
2. PInfer's raw formulas get the **LLM ranking + judging + naming** layer.
3. PInfer's verified invariants **seed the agentic proposer** (few-shot → fewer hallucinated events).
4. The agentic layer **auto-generates PInfer hints** (UG1 event combos, UG2 custom predicates,
   UG3 state exposure) — automating the paper's 15/43 manual-guidance cases.

## 3. Dependency & terminology contract (corrects the v1 "portable = no C#" error)

There are **three capability tiers**, not two. "Portable" means *no PInfer enumerator and no Z3*
— it does **not** mean "no C#": the validation oracle *is* C#.

| Tier | Requires | Provides | Notes |
|---|---|---|---|
| **Base** (mandatory) | .NET 8 SDK + the `p` global tool (PCompiler + PChecker); Python 3; an LLM agent runtime + credentials | propose → prep → repair → validate → falsification-prune → rank → judge | `invariant_core.py` shells out to `p compile`/`p check`; `build_env()` probes `dotnet`. This is the whole milestone. |
| **+Z3 plug-in** | Z3 built with .NET bindings, on `LD_LIBRARY_PATH` | implication-based semantic pruning (`SMTWrapper.CheckImplies`) | Cuts redundant-but-true specs the Base tier keeps. Optional. |
| **+PInfer enumerator** | Full `experimental/pinfer` toolchain: rebased onto master, Java 22 + Maven, Daikon, vendored `pinfer-dependencies/*.jar`, built C# backend | a 3rd proposer (enumerative + Daikon) whose emitted `.p` monitors merge into the candidate set | Amazon-Linux-tested only. Optional plug-in; detection in §6.5. |

**Base-tier prerequisite (Phase 0):** install .NET 8, run `p install`, confirm `p compile`/`p check`
work on a tutorial project. No Java/Z3/Daikon needed.

## 4. Target architecture

```
                        ┌──────────────── PROPOSERS (pluggable) ────────────────┐
  design intent/docs ─► │ B. Agentic templated (propose_templated.js)           │ [BASE]   ─┐
  source (PSrc/PTst) ─► │ C. Agentic intent   (intent-lens proposer, NEW)       │ [BASE]    │
  P model + traces ───► │ A. PInfer enumerative (C#, grid + Daikon)             │ [PLUG-IN] │
  (seeds, Phase 6)      └────────────────────────┬───────────────────────────────┘          │
                                                 ▼                                candidates (unified schema §5)
      ┌──────────────────────── SHARED BACKBONE (invariant_core.py) ─────────────────────────┐
      │ 1. PREP        deterministic P fixups + AUTO-CANARY generation (prep_candidates.py)    │ [BASE]
      │ 2. STATIC GATE event/field-name validation vs declarations (reuse PeasyAI validators)  │ [BASE]
      │ 3. REPAIR      diag → LLM repair → verify  (compile yield ~72%→~100%)                  │ [BASE]
      │ 4. VALIDATE    PChecker per candidate → HOLDS-BOUNDED / FAILS+cex / VACUOUS /          │ [BASE]
      │                UNKNOWN-VACUITY / INCONCLUSIVE / COMPILE-ERR  (baseline-diff attribution)│
      │ 5. PRUNE       Base: dedup(structured key) + falsification;  +Z3: semantic subsumption │ [BASE/+Z3]
      │ 6. RANK        schema-native scoring kernel (ported from ranking_with_llm.py)          │ [BASE]
      │ 7. JUDGE       LLM over cex → {verdict, confidence, cex-grounding, development_action}  │ [BASE]
      └────────────────────────┬──────────────────────────────────────────────────────────────┘
                               ▼
              ┌────────── DOWNSTREAM (Phase 5, deferred) ──────────┐
              │ Verification: TransformToP → PVerifier              │
              │ Monitoring:   auto-gen runtime monitors             │
              │ Testing:      test-coverage-gap (φ_L ⇒ φ_U) report  │
              └─────────────────────────────────────────────────────┘
```

## 5. Canonical candidate schema (frozen in Phase 1 — the linchpin, corrects C3)

The v1 dedup key `(observes, guard, relation)` referenced fields that do not exist. The schema
must carry a **structured formula record** alongside the opaque `specCode` so dedup/merge/rank
are well-defined and every proposer (incl. the PInfer adapter) can populate it.

```jsonc
Candidate {
  name: string,                    // unique per run
  intent: string,                  // NL description
  category: string,
  provenance: "templated"|"intent"|"enumerative",   // NEW
  observes: string[],              // event types the monitor observes
  // structured formula record (NEW — enables dedup, SC, ranking):
  formula: {
    quantifiers: [{var, type, kind: "forall"|"exists"}],   // arity = count
    guards: string[],              // conjunct predicates (canonicalized)
    relations: string[],          // conjunct predicates asserted to hold
    sc: null | {op: "=="|">"|">="|"<="|"count", bound: string},  // quorum/cardinality (M3)
    config_event: string|null,     // population source for SC (M3)
    uses_index: bool               // happens-before present (grammar-fragment marker)
  },
  specCode: string,                // compilable `spec ... observes ... { }` P monitor
  canary: string|null,             // auto-generated companion (see §6.1)
  predicted_bucket: "verified"|"bug"|"spurious"|"vacuous",
  // filled by the backbone, not the proposer:
  verdict: Verdict|null,           // see §6.4
  provenance_run: {model, temperature, prompt_hash, iters, seed}   // reproducibility (M6/M7)
}
```

- **Dedup key** = canonicalized `(sorted observes, sorted guards, sorted relations, sc,
  quantifier-kinds)`. On collision, **cluster and keep a representative** (do not silently drop —
  record the cluster for metrics). A Python dataclass mirrors this; a **JS↔Python parity test**
  guards drift between `propose_templated.js`'s `CAND_SCHEMA` and the dataclass (N1).
- **Freeze gate (Phase 1 exit):** schema, dedup-key function, and judge output schema are pinned
  and unit-tested before any proposer or backbone stage is built against them.

## 6. Backbone stages in detail (the parts v1 hand-waved)

### 6.1 Vacuity is a backbone responsibility, not a proposer favor (M4)
v1 only detected VACUOUS when the *LLM* emitted a `_canary`; PInfer and the templated proposer
emit none, so genuinely-vacuous candidates were reported HOLDS→"verified."
- **PREP auto-generates a canary per candidate** by negating the asserted relation inside the
  same guarded branch (guarantees guard-identity), independent of the proposer.
- A candidate with no derivable canary is classified **UNKNOWN-VACUITY**, never HOLDS.
- The canary must **trip within the same `--iters` budget**; otherwise we cannot distinguish
  "guard unreachable" (vacuous) from "guard under-scheduled" (bounded-checking artifact).

### 6.2 Static name gate before model-checking (M9)
Compile-success + HOLDS ≠ meaningful spec: a hallucinated event widened to a real-but-wrong one,
or a type-compatible wrong field (`.trial` vs `.attempt`), compiles and HOLDs vacuously.
- Add a deterministic gate that parses each candidate's `observes` + every payload-field access
  and validates them against the benchmark's real event/type declarations.
- **Reuse PeasyAI's `EventDeclarationValidator` and `PayloadFieldValidator`**
  (`Src/PeasyAI/src/core/validation/validators.py`); promote field-mismatch to a hard reject.

### 6.3 Bug attribution is baseline-differential, not substring matching (M5)
v1 attributed monitor-vs-SUT failures by grepping the last 160 chars of the trace for
`_candidates.p`/`liveness`/`hot state` — a monitor-caused **deadlock** ("Deadlock detected")
matches none of these and gets mislabeled `sut`→INCONCLUSIVE→dropped, discarding real bugs.
- Run `p check` once on the **unmodified** project; record pre-existing SUT failures.
- Attribute a candidate failure to the monitor **iff it is absent from that baseline**.
- Parse structured PChecker output / the failing spec name rather than a truncated message.
- Regression fixture: a monitor-caught bug whose cex omits the candidate filename.

### 6.4 Bounded verdicts are labeled as such (M6)
`p check -i N` is bounded exploration, not a proof. The verdict enum is:
`HOLDS-BOUNDED` (provisional; records `iters`+`seed`) · `HOLDS-PROVEN` (PVerifier only, Phase 5) ·
`FAILS`(+cex) · `VACUOUS` · `UNKNOWN-VACUITY` · `INCONCLUSIVE` · `COMPILE-ERR`.
- Nothing may be labeled "verified/sound" on `HOLDS-BOUNDED` alone.
- **Iteration-sensitivity gate:** rerun the accepted set at 2–3× budget; report any verdict flips.

### 6.5 PInfer plug-in adapter is explicit work, not a one-liner (M2/M14, N4)
- Detection: probe `p infer --help` exit code **and** presence of Z3 + `daikon.jar` (not just
  "`p infer` exists").
- The adapter **consumes PInfer's emitted `spec…observes` monitors** (`TransformToP.WriteSpecMonitor`,
  confirmed to exist) plus metadata, back-fills `intent`/`category`/`formula`/`provenance`,
  normalizes naming/`observes` so `SPEC_RE`/`_wire` ingest them. (It does *not* synthesize
  monitors from formulas — that premise was wrong.)
- **Also expose PInfer's event-combination list** (`ExploreFunction`/`AddHint` — pure C# AST
  analysis, no Z3/Daikon/traces) as a lightweight signal to feed the LLM proposers a
  quantification skeleton — the cheapest high-value fidelity restoration for M2.

### 6.6 Optional grammar-conformance check (M13)
A Phase-1 checker parses `specCode` → canonical tuple and flags predicates outside the shared
fragment, so "overlapping fragments" is enforced/observed rather than merely asserted.

## 7. Phased implementation (Base tier unless noted)

### Phase 0 — Consolidate the Base tier
- Branch off `master`; bring in the agentic scripts from `pinfer-peasyai-design`
  (`invariant_core.py`, `propose_templated.js`, `prep/diag/check/repair/verify`).
- Merge scope is **only the two agentic branches** (small); the `experimental/pinfer` rebase is a
  *plug-in* prerequisite, deferred (N4).
- Base prereqs installed (§3); PInfer enumerator/Z3 **not** required.
- **Exit (split):** (a) the deterministic middle (prep/diag/check/verify) runs end-to-end on one
  tutorial benchmark; (b) a full propose→check→judge runs once with a live LLM (names the agent
  runtime + credentials in §9). *Does not claim "no C#."*

### Phase 1 — Freeze the shared core + schema (enlarged vs v1)
- `invariant_core.py` consolidates existing pieces (dedup the 3 `build_env` copies) **and** adds
  net-new: the `Candidate` dataclass (§5), the dedup-key function, the judge output schema,
  provenance-run fields.
- Adopt + **extend** `propose_templated.js`'s `CAND_SCHEMA` with the structured `formula` record,
  `provenance`, `config_event`, `sc`, `uses_index`.
- `check_candidates.py`/`diag_compile.py`/`verify_repairs.py` become thin CLIs over the core.
- **Exit (testable):** schema (de)serialization round-trip test; dedup-key unit test; judge-schema
  test; JS↔Python schema parity test; `validate_candidates` classification test with mocked
  `_check_one`/`_wire` covering **every** verdict branch (incl. auto-canary vacuity, UNKNOWN-VACUITY,
  baseline-diff INCONCLUSIVE). (v1's "test_invariant_core.py green" was hollow — N6.)

### Phase 2a — Intent proposer (net-new, was hidden in v1)
- Build the intent-lens proposer as a runnable proposer against the frozen schema, reusing the
  existing `INTENT_LENSES`/`propose_*_prompt()` strings (do **not** duplicate prompt logic).
- Clarify how it differs from `propose_templated.js` (soft NL lenses vs grammar scaffold) so it
  isn't a near-duplicate.
- **Exit:** emits schema-valid candidates on one benchmark.

### Phase 2b — Hybrid merge + PInfer hook
- `hybrid` merge: dedup by the structured key (§5), cluster-and-keep representatives, union.
- PInfer plug-in adapter (§6.5) merges enumerative candidates **when the tier is present**;
  skipped with a logged notice otherwise.
- **Exit:** one merged, deduped candidate set from ≥2 Base proposers; PInfer candidates ingest
  correctly on the plug-in box (verified once on FD).

### Phase 3 — Unified pruning (graceful degradation)
- Base: structured dedup + PChecker falsification.
- +Z3: implication-based semantic subsumption (`SMTWrapper.CheckImplies`) runs first to cut the set.
- **Exit (subset/partition claim, corrected — M1):** the +Z3 set ⊆ the Base set (Base keeps
  redundant-but-true specs Z3 drops); every candidate the Base path *rejects* is also rejected by
  the +Z3 path; **reduction ratio reported per-path with absolute in/out counts** (not asserted
  equal). The equality-style comparison is a plug-in-present gate, not part of Base acceptance.

### Phase 4 — Rank + judge
- **Rank:** port the *scoring kernel* (`compute_score` weighting + 4-metric rubric) from
  `ranking_with_llm.py` into a schema-native ranker consuming `Candidate`/`Verdict`. **Drop**
  Strands, the `constants` module, the Bedrock model id, and the `pruned_invariants.txt` input;
  route the LLM through the wrapper-owns-the-provider seam (M12). Cross-branch port — real work.
- **Judge:** rewrite `JUDGE_RUBRIC`/`judge_user_prompt` to emit
  `{verdict, confidence, cex-grounding, development_action}`, action ∈
  {add-to-tests, debug-bug, drop-spurious, drop-vacuous}. Reconcile `vacuous` ownership
  (VALIDATE computes it via canary; JUDGE only classifies FAILED candidates). **Remove**
  `relax-test-coverage-gap` (depends on §10 Phase-5 work).
- **Exit:** one ranked+judged report on the acceptance benchmarks (§9); judge-contract unit test.

## 8. Reproducibility & evaluation methodology (net-new — M7/M8/M15/M16)

LLM propose/repair/judge and `p check` are non-deterministic; a single-shot table proves nothing.

- **Pinning:** fix model id + temperature (0 where allowed); record `prompt_hash`, model,
  temperature, and the **PChecker `--seed`** in each candidate's `provenance_run`. Cache raw LLM
  responses; commit a **golden candidate set** per benchmark so the deterministic
  `classify()`/prune/rank layer is CI-gatable independent of live LLM calls.
- **Variance:** run acceptance over **N ≥ 3 seeds**; report the distribution and verdict-set
  stability (Jaccard across runs), not just one number.
- **Baseline ablation (M8):** compare the full pipeline against "LLM-propose-once, no
  prep/repair/validate/prune/rank/judge" on the same benchmarks — isolates the value of the loop.
- **Metrics table (M8)** with formulas + reported values (gate only where noted):
  - **false-bug rate** = confirmed-spurious among `bug` verdicts (primary quality metric).
  - **reduction ratio** = candidates in/out of PRUNE (absolute counts + ratio, per path).
  - **goal-spec recall** = |recovered| / |authored| (denominators: FD=1, TPC=2).
  - cost (tokens) + wall-clock — reported, not gated.
- **Rediscovery oracle (M16):** "recovers the authored spec" = **bounded mutual implication**
  checked by PChecker (`candidate ∧ ¬authored` and `authored ∧ ¬candidate` both HOLD-BOUNDED under
  a documented budget), reusing the Verdict pipeline. Fallback: a logged manual candidate→authored
  mapping, explicitly labeled manual.
- **Fault-injection benchmark (M15):** since FD and TPC are *correct*, the `bug` branch is never
  exercised. Add a mutated variant (e.g. break FD's crash-notification path, or TPC's
  abort-on-any-NO rule) and require JUDGE to label the resulting failure `bug` with a cex-grounded
  rationale, **and** to *not* label the correct baseline `bug` (false-positive control).

## 9. Interfaces (this milestone)
- **Base CLI:** `python3 check_candidates.py` (exists) + a top-level `learn_invariants.py`
  orchestrator over the core (prep/gate/repair/validate/prune/rank/judge). Proposal itself runs
  inside the **agent runtime** (the `.js` proposers use `agent()`/`parallel()`/`phase()` globals),
  which needs an LLM provider + credentials (PeasyAI venv + `~/.peasyai/settings.json`, or the
  agent SDK) — named here so Phase 0(b) is provisionable.
- Wire points for the two agent surfaces (**PeasyAI MCP `tools/invariants.py`**,
  **`.claude/skills/learn-invariants`**) stubbed to call the core — full surface build deferred.
- PInfer C# CLI stays a plug-in, not a dependency.

## 10. Explicit non-goals (deferred to Phases 5–6)
- **Phase 5 — Downstream:** PVerifier `TransformToP` translation (`HOLDS-PROVEN`), runtime-monitor
  generation, and the **test-coverage-gap (`φ_L ⇒ φ_U`) report** (moved out of milestone
  acceptance per C1). `φ_L ⇒ φ_U` needs implication reasoning (Z3 or a caveated LLM approximation)
  and a defined `φ_L` source — both belong here.
- **Phase 6 — Cross-feeding:** seeding agentic proposers with PInfer invariants, auto-hint
  generation, and the enumerative-vs-agentic head-to-head that would *validate* the §2
  complementarity claim.
- Building/shipping the full MCP tool + skill surfaces.

## 11. Risks
1. **Platform:** PInfer enumerator "tested on Amazon Linux only," needs hand-built Z3 + Java 22 +
   Maven + rebasing `experimental/pinfer` (3 ahead / 42 behind master). Base tier avoids all of
   this; plug-in tiers own it.
2. **Soundness contract:** only `HOLDS-PROVEN` (PVerifier) is sound; `HOLDS-BOUNDED` and all LLM
   proposals/judgments are provisional. Enforced in verdict labels (§6.4) and report copy.
3. **Bounded checking:** finite `--iters` can miss a counterexample (false HOLDS) *and*
   under-schedule a guard (false VACUOUS). Mitigated by the iteration-sensitivity gate and the
   canary-must-trip rule.
4. **Attribution robustness:** monitor-vs-SUT misattribution silently drops bugs; mitigated by
   baseline-differential attribution (§6.3) with a regression fixture.
5. **Vacuity/hallucination:** compile-success + HOLDS ≠ meaningful spec; mitigated by backbone
   auto-canaries (§6.1) and the static name gate (§6.2).
6. **Portable recall:** the LLM proposers have no send/recv/goto reachability analysis, so
   cross-machine causal pairs may be missed; mitigated by feeding PInfer's combo skeleton when the
   plug-in is present (§6.5), otherwise an accepted recall downgrade.
7. **Complementarity unvalidated:** §2's central bet is not measured by this milestone (PInfer runs
   only on the plug-in box); §8 begins measuring it, full head-to-head is Phase 6.
8. **Non-determinism / reproducibility:** addressed by §8 (pinning, seeds, golden set, variance).
9. **Schema drift:** JS `CAND_SCHEMA` vs Python dataclass — guarded by the parity test (§5, N1).
10. **Cost / fan-out:** propose (shapes × lenses × candidates) + repair rounds + per-candidate
    `p compile`/`p check` + per-failure judge. Mitigations: max-candidates-per-benchmark cap, hard
    repair-round cap K, shared iters/time budget, cost accounting in the report, and **bisect**
    (not linear scan) to isolate an offending candidate when batch compile fails.

## 12. Acceptance for the milestone (corrected — C1/M8/M15/M16)
On `4_FailureDetector` + `2_TwoPhaseCommit` (Base tier), plus one **fault-injected** variant,
run over **N ≥ 3 seeds** with pinned model/temperature/PChecker-seed and produce a ranked, judged
report such that:
- **(a) Recall:** each project's authored spec is recovered, measured by the bounded
  mutual-implication oracle (§8). Denominators FD=1, TPC=2. Report per-seed distribution.
  *Note:* TPC's `AtomicityInvariant` needs the SC/cardinality shape (§5, M3) — its recovery is the
  test that SC support works.
- **(b) Classification:** every model-checker failure is classified
  `bug`/`spurious`/`vacuous`/`UNKNOWN-VACUITY` with a cex-grounded rationale; on the fault-injected
  variant the injected fault is labeled `bug` and the correct baseline is **not**.
- **(c) Metrics:** the §8 table is emitted — false-bug rate, per-path reduction ratio (absolute
  counts), recall — alongside the propose-once ablation baseline.
- **Explicitly NOT required at this milestone:** the test-coverage-gap (`φ_L ⇒ φ_U`) finding
  (moved to Phase 5), PVerifier proofs, and any PInfer-enumerator/Z3 result (plug-in tiers).

## 13. v1 → v2 finding map
- **C1** (§9(c) required deferred work) → moved test-gap to §10 Phase 5; §12 excludes it.
- **C2** (false "no C#") → §3 dependency tiers; labels renamed `[BASE]`/`[+Z3]`/`[PLUG-IN]`.
- **C3** (dedup key fields missing) → §5 structured `formula` record + freeze gate.
- **M1** (Phase 3 "identical verdicts") → §7 Phase 3 subset/partition exit.
- **M2** (causal-combo selection dropped) → §2 table row + §6.5 combo-skeleton + §11 R6.
- **M3** (quorum/SC missing) → §5 `sc`/`config_event`; §12(a) TPC gate.
- **M4** (vacuity proposer-dependent) → §6.1 backbone auto-canary + UNKNOWN-VACUITY.
- **M5** (brittle attribution) → §6.3 baseline-differential.
- **M6** (bounded HOLDS as sound) → §6.4 `HOLDS-BOUNDED`/`HOLDS-PROVEN` + sensitivity gate.
- **M7** (non-reproducible) → §8 pinning + N≥3 seeds + golden set.
- **M8** (no baseline/metrics) → §8 ablation + metrics table; §12(c).
- **M9** (hallucination survives) → §6.2 static name gate (PeasyAI validators).
- **M10** (intent proposer missing) → §7 Phase 2a split.
- **M11** (judge lacks development_action) → §7 Phase 4 judge rewrite + Phase 1 schema freeze.
- **M12** (ranking near-rewrite) → §7 Phase 4 scoring-kernel port + provider seam.
- **M13** ("same grammar" false) → §2 "overlapping fragments" + §6.6 conformance check.
- **M14** (complementarity untested) → §11 R7; §10 Phase 6 head-to-head.
- **M15** (no bug in benchmarks) → §8 fault-injection; §12(b).
- **M16** (rediscovery unmeasurable) → §8 bounded mutual-implication oracle.
- **N1–N6** (schema drift, harness runtime, Daikon numerics caveat, plug-in detection, cost budget,
  hollow tests) → §5 parity test, §9 runtime, §2 table, §6.5/N4, §11 R10, §7 Phase 1 exit.
