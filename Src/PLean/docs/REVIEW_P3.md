# PLean Phase 3 — Code Review (third pass)

Scope: the working-tree changes on `dev/p-lean` against `master`, read against [`docs/PLAN_P3.md`](PLAN_P3.md) and [`docs/STATUS.md`](STATUS.md). This is the third review pass:
- Pass 1 (2026-06-06) — found §1.1–§1.4 blockers, §2.1–§2.6 deviations, §4 / §5 housekeeping items.
- Pass 2 (2026-06-06) — verified pass-1 fixes; sharpened §1 / §A.1 / §A.3 / §2.4 / §B.2 / §B.3 / §C / §6.7.
- Pass 3 (this) — assesses the second-pass follow-up sweep summarised by the author and recorded in [`STATUS.md` §"REVIEW_P3 second-pass follow-up sweep"](STATUS.md).

> **TL;DR (3rd pass).** Every item the second-pass review flagged as a sharpened blocker has been addressed in code and the documentation has been brought into agreement with what shipped. The remaining gaps are all explicit deferrals (D27 wrapper, D28 mini-tactic case-table, `loom_smt`, registry-aware `is`-macro) that are now documented as "Phase 3 implementation deviation" callouts in PLAN_P3 and listed under STATUS's "Deferred from REVIEW_P3" — i.e., they are no longer the silent gaps the previous pass flagged. **Phase 3 infrastructure is ready to ship as `Phase 3a`** once the M3 benchmarks unblock R15. Recommendations below are minor polish + a few residual housekeeping items, none of them ship-blockers.

---

## 1. Verified-fixed items (3rd pass)

Each second-pass blocker was re-checked against the working tree.

### 1.1 §1 — `default_inv` head-symbol-gated and wired into `pverify` ✓

[`Tactic.lean:75-97`](../PLean/Verify/Tactic.lean#L75-L97) declares both `default_inv_guard` and `default_inv`. The guard is an `elab_rules` tactic that:

```
let head := goal.consumeMData |>.getAppFn
let some headName := head.constName? | throwError ...
let allowed := [``DefaultInvariants, ``UniqueActions, ``IncreasingCount, ``ReceivedSubsetSent]
unless allowed.contains headName do throwError ...
```

`default_inv` invokes `default_inv_guard` as its first step ([Tactic.lean:105](../PLean/Verify/Tactic.lean#L105)) so a non-default-shaped goal trips the guard before `refine ⟨?_, ?_, ?_⟩` mis-fires. With that in place, `pverify` now safely chains `(first | default_inv | pverify_solve)` ([Tactic.lean:242](../PLean/Verify/Tactic.lean#L242)). The over-fire risk that motivated *not* wiring it into `pverify` in pass 1 is genuinely gone. Resolves §1.

A small follow-up worth noting: `default_inv_guard` looks at the *current* goal head; if `pverify` has just done `try intros`, the head is right. If a future caller (or the obligation generator) lands a goal in a state where the head is `∀` or `→` (binders not yet `intro`'d), the guard's `getAppFn` returns the binder constant and the guard fails. That's *correct* fail-fast behaviour for the present call sites, but if some future tactic wants to call `default_inv` *before* intros, this guard will mis-fire. Worth a one-line comment near `default_inv_guard` flagging "callers must `intros` first".

### 1.2 §A.1 — theorem-name collisions across `Proof` blocks ✓

[`Obligation.lean:117-128`](../PLean/Verify/Obligation.lean#L117-L128) now constructs theorem names as

```
<M>.<S>.<ev>_correct_<proofTagOrBlockN>_<target>[_using_<L1>_<L2>...]
```

with `proofTagStr := if proofTag == anonymous then s!"block{proofIdx}" else proofTag.toString`. The `proofIdx` is threaded through `synthesise` from the [for hProof : proofIdx in [0:ctx.proofs.size]](../PLean/Verify/Obligation.lean#L376) loop. Two anonymous `Proof { prove safety; }` blocks now emit `..._correct_block0_safety` and `..._correct_block1_safety` respectively; named blocks (`Proof Block1 { ... }`) embed `Block1` directly. The §A.1 collision shapes are no longer reachable.

`ObligationShape.lean` exercises both flavours (`Proof Block1 { prove safety; }` + `Proof Block2 { prove default; }`) and `#guard_msgs`-pins the resulting names — confirmed at [`ObligationShape.lean:60`](../Tests/Surface/ObligationShape.lean#L60), [line 74](../Tests/Surface/ObligationShape.lean#L74). Resolves §A.1.

### 1.3 §A.3 — `DefaultInvariants` in pre AND post ✓

[`Obligation.lean:152-159`](../PLean/Verify/Obligation.lean#L152-L159) builds `defaultPred ← \`(PLean.DefaultInvariants)` and folds it into both the precondition and postcondition conjunctions:

```
let preds : Array (TSyntax `term) :=
  #[lemmaPred] ++ usingPreds ++ #[defaultPred, ← `($initsId)]
```

```
let postBody ← buildConjAt #[lemmaPred, defaultPred, ← `($initsId)] sId
```

`prove safety;` obligations now require + preserve `DefaultInvariants` symmetrically. For `prove default;` directives `lemmaPred` *is* `DefaultInvariants`, so the conjunction has it twice — harmless but the `ObligationShape.lean` snapshot shows the duplication explicitly (`DefaultInvariants s ∧ DefaultInvariants s ∧ ...` at [line 77](../Tests/Surface/ObligationShape.lean#L77)). Resolves §A.3.

A purely cosmetic note: when the user wrote `prove default;`, the duplicate `DefaultInvariants ∧ DefaultInvariants` is technically redundant but doesn't change the proof obligation. Could be dedup'd in a follow-up by special-casing `isDefault` to skip adding `defaultPred` again — saves one `simp` step but otherwise no impact.

### 1.4 §2.4 — dead `idGS` / `idPP` removed ✓

[`Obligation.lean:48-50`](../PLean/Verify/Obligation.lean#L48-L50) replaces the dead idents with a comment explaining they were redundant (obligations reach `GS` and `PProp` via `PLean.GlobalState $idSig` / `PProp $idSig` directly). Clean.

### 1.5 §2.6 — `default_inv` uses `simp only` ✓

[`Tactic.lean:115-133`](../PLean/Verify/Tactic.lean#L115-L133) replaces every `simp [...]` with `simp only [...]`. The drift-risk from a future mathlib4 `[simp]` lemma touching `decide` / `Nat.lt` is closed for the default-invariant path. The `pverify_wp` step ([Tactic.lean:179-181](../PLean/Verify/Tactic.lean#L179-L181)) was already `simp only`. The user-facing `pverify_solve` chain still terminates in `grind` / `tauto`, which have their own implicit simp sets, but those are intentional and bounded by the tactics' own contracts.

### 1.6 §2.3 — `DispatcherContract.lean` import dropped ✓

[`Obligation.lean:33-38`](../PLean/Verify/Obligation.lean#L33-L38) replaces `import PLean.Verify.DispatcherContract` with a NOTE explaining the file is retained for docstring purposes only. [`PLean.lean:23-27`](../PLean.lean#L23-L27) explicitly does *not* re-export `DispatcherContract`, with a comment pointing to REVIEW_P3 §2.3. The file lives, but only as documentation; no live code path imports `buildDispatcherContractTerm`. The "code that loads but doesn't run" smell is gone.

### 1.7 §B.2 — spec-machine skip is now `logInfo`-d ✓

[`Obligation.lean:381-385`](../PLean/Verify/Obligation.lean#L381-L385):

```
if m.isSpec then
  logInfo m!"spec machine `{mname}` skipped — Phase 4 owns spec obligations"
  continue
```

A user with a `Proof { prove X; }` directive that hits a spec machine now sees the skip rather than getting silent zero-obligation behavior.

### 1.8 §B.3 — env-rollback caveat documented ✓

[`Obligation.lean:422-433`](../PLean/Verify/Obligation.lean#L422-L433) now documents both leaks (message-log async-snapshot + env state from a partially-elaborated `theorem` decl). The implementation still rolls back messages only — but the doc no longer claims it does anything more.

### 1.9 New tests — `Phase3Errors.lean` and `ObligationShape.lean` ✓

[`Tests/Surface/Phase3Errors.lean`](../Tests/Surface/Phase3Errors.lean) `#guard_msgs`-pins the four error paths (`Lemma default`, `Theorem default`, `prove <unknown>;`, `prove ... using <unknown>;`). [`Tests/Surface/ObligationShape.lean`](../Tests/Surface/ObligationShape.lean) `#guard_msgs`-pins both the §A.1 Proof-tag suffix scheme and the §A.3 `DefaultInvariants`-in-post invariant. Future refactors that weaken any of these silently fail CI.

A small caveat on `Phase3Errors.lean`: `pmodule Phase3ErrLemmaDefault ... end` is declared *twice* (one empty body to claim the namespace, one to host the failing `Lemma default {…}` under `#guard_msgs`). The pattern is necessary — `#guard_msgs in <command>` wraps a single command, and a `Lemma`-bearing pmodule with the same name would have its `pmodule` opener also captured. The double declaration is correct but a comment "double `pmodule` is intentional — keeps `#guard_msgs` scoped to just the `Lemma`" would help the next reader.

### 1.10 §C / §6.6 — Phase2PingPong rewrite ✓

[`Tests/Surface/Phase2PingPong_manual.lean`](../Tests/Surface/Phase2PingPong_manual.lean) preserves the original M2 hand-written triples under `pmodule Phase2PingPongManual`. [`Tests/Surface/Phase2PingPong.lean`](../Tests/Surface/Phase2PingPong.lean) now uses `Theorem trivial_safety { invariant always_true : ∀ s, True }` + `Proof Safety { prove trivial_safety ; }`. The `#pverify Phase2PingPong` invocation is commented out at [line 71](../Tests/Surface/Phase2PingPong.lean#L71) with a clear explanation of the R15 dependency. Both files build clean.

This isn't quite the M2-equivalent end-to-end smoke test that pass-2 §C wanted (the headline `PongAfterPing` invariant doesn't ride along), but it *is* sufficient to exercise the obligation generator + the Theorem/Proof pipeline against a non-trivial pmodule (one handler with `send`, one with `pure ()`). When R15 lands, swapping `trivial_safety` for `PongAfterPing` is a one-block edit.

### 1.11 §6.7 — `ObligationShape.lean` ✓

Already covered in §1.2. The file lands and pins the post-§A.1/§A.3-fix obligation shape.

### 1.12 §5.1 — STATUS "build green" wording ✓

[`STATUS.md:300-316`](STATUS.md#L300-L316) replaces "all 1000 build targets remain green" with a precise breakdown: which test files exercise `#pverify`, which exercise `#pwf` only, and an explicit "'Build green' therefore means 'syntax / well-formedness / obligation-shape regressions all hold', not 'every M3 obligation closes' — that's gated on R15." Resolves §5.1.

### 1.13 §5.2 — PLAN_P3 deviation callouts ✓

PLAN_P3 now has four "Phase 3 implementation deviation" callouts:

- **D20** at [PLAN_P3.md:181](PLAN_P3.md#L181) — `is` macro is not registry-aware
- **D22** at [PLAN_P3.md:252](PLAN_P3.md#L252) — `pverify using ...` / `pverify!` / `pverify?` not implemented; `loom_smt` not wired
- **D27** at [PLAN_P3.md:341](PLAN_P3.md#L341) — `_handler_wrapped` not built; existential precondition is the live design
- **D28** at [PLAN_P3.md:363](PLAN_P3.md#L363) — `default_inv` is head-symbol-gated `simp only + omega`, not the bounded mini-tactic case-table

Each callout names the REVIEW_P3 section that flagged it and points to STATUS for the live deferral list. The plan no longer lies-by-omission. Resolves §5.2.

---

## 2. New observations from this pass

### 2.1 `synthesise` "messages" rollback can mask real warnings

[`Obligation.lean:434-438`](../PLean/Verify/Obligation.lean#L434-L438) inspects the message log between emissions and, on `hasError`, writes back `{ st with messages := savedSt.messages }`. The intent is right — a tactic-level error in obligation N shouldn't poison obligation N+1's report. But the rollback restores *all* messages including warnings that obligations 1..N-1 emitted (e.g., the §B.2 spec-machine skip log info). Concretely: a single failing obligation can erase every prior `logInfo` in the same `#pverify` invocation.

**Action.** Either (a) only roll back errors (preserve info / warning entries from `savedSt.messages` to `curSt.messages`), or (b) deliberately re-emit prior info-level messages after the rollback. Current code chooses neither; the practical effect is "if any obligation fails, the report's prior progress info is gone". Low-impact for most cases (the user is still going to read the failed-obligation message), but worth tightening when convenient.

### 2.2 `default_inv_guard`'s error message names a constant the user may not have

[`Tactic.lean:97`](../PLean/Verify/Tactic.lean#L97):

```
throwError "default_inv: goal head '{headName}' is not a default-invariant constant"
```

This fires when the guard rejects a non-default goal — which is exactly when `pverify`'s `first | default_inv | pverify_solve` falls through to `pverify_solve`. The error gets caught by the outer `first`, but if a curious user calls `default_inv` directly on a user invariant, they see a `default_inv: goal head 'safety'` message. That's confusing without context — `safety` is a perfectly fine user-defined predicate; the *guard* is what's rejecting it. Suggest:

```
throwError "default_inv: applies only to UniqueActions / IncreasingCount / ReceivedSubsetSent / DefaultInvariants goals; this goal's head is '{headName}'"
```

— small wording polish.

### 2.3 `Phase3Errors.lean` doesn't pin the §A.1 collision-error path

The §A.1 fix made collisions impossible, so there's no error to `#guard_msgs`-pin. But the converse is also useful: a *positive* test that two-Proof-blocks-same-target builds clean. `ObligationShape.lean` does this implicitly (it has `Proof Block1 { prove safety; }` + `Proof Block2 { prove default; }`), but a side-by-side `Proof B1 { prove safety; }` / `Proof B2 { prove safety; }` test would explicitly exercise the §A.1 disambiguation. Consider adding a 30-line `Phase3DuplicateTarget.lean`.

### 2.4 `default_inv` still uses the older simp-only-plus-omega shape, not D28's mini-tactic table

This is explicitly deferred per the new D28 deviation callout in PLAN_P3 ([line 363](PLAN_P3.md#L363)). The deferral is fine for M3 ("works for M1/M2/DistributedLock-style no-`send` or one-`send` handlers") — the worry from pass 2 was that the deferral was *silent*. It isn't silent now. Track for Phase 3b.

### 2.5 `Phase3Errors.lean`'s `pmodule Phase3ErrLemmaDefault` double-declaration may not work as intended

Lines 20–21 declare `pmodule Phase3ErrLemmaDefault ... end Phase3ErrLemmaDefault` — an empty pmodule, presumably to claim the namespace. Lines 23–31 then re-open the same `pmodule Phase3ErrLemmaDefault` with the failing `Lemma default { ... }`. The doc-comment at lines 8-12 explains:

> `#guard_msgs in` wraps a *single* command. We keep each error case in its own `pmodule` and place the `#guard_msgs in <command>` against the specific declaration that should fail. (Wrapping a whole `pmodule M ... end M` block fails to capture diagnostics that elaborate at a child position.)

OK, the doc explains the pattern, but the empty pre-declaration is doing nothing structurally — opening a pmodule, immediately closing it, then opening it again. The `Lemma default` would fail just as well in the *first* pmodule:

```lean
pmodule Phase3ErrLemmaDefault
/-- error: ... -/
#guard_msgs in
Lemma default { invariant t : ∀ _ : GlobalState Sig, True }
end Phase3ErrLemmaDefault
```

would work. The empty pre-declaration looks like a refactor leftover. Worth simplifying — remove the empty `pmodule`/`end` pair and combine into one block per error case.

### 2.6 The R20 `kind ≠ 0` predicate is not exercised by any test

The pass-1 §5.3 fix added `kind ≠ 0 ∧ kind = <M>_kind` to `<M>_allocated`. `Phase3DistributedLock.lean` uses `Node_allocated` directly in its invariants, but the *new* `kind ≠ 0` clause isn't probed (the test passes `#pwf` only; the obligation never tries to derive the predicate against an actually-allocated machine). A small hand-rolled test that constructs a `default`-initialised `MachineState` and confirms `<M>_allocated` returns `False` would verify the R20 mitigation. Low priority — the predicate definition is small enough to read by inspection — but worth noting.

---

## 3. Items still deferred (per STATUS / PLAN_P3 — confirmed)

| # | Item | Disposition | Tracked where |
|---|---|---|---|
| §2.1 | D27 `_handler_wrapped` form | Deferred to Phase 3b or doc-rewrite | PLAN_P3 D27 deviation, STATUS deferred |
| §2.2 | D28 mini-tactic case-table | Deferred to Phase 3b | PLAN_P3 D28 deviation, STATUS deferred |
| §2.5 | `loom_smt` SMT fallback | Deferred until first benchmark needs it | PLAN_P3 D22 deviation, STATUS deferred |
| §2.6 | Registry-aware `is` macro | Deferred — typos surface as generic Lean errors | PLAN_P3 D20 deviation, STATUS deferred |
| §6.8 | M3 benchmark end-to-end | Gated on R15 (per-accessor `#derive_lifted_wp` + per-primitive `loomSpec`) | STATUS open follow-ups |

All four deferrals are now (a) named in STATUS as explicit deferrals, (b) have a "Phase 3 implementation deviation" callout in PLAN_P3 explaining what shipped vs what was planned, and (c) link back to the REVIEW_P3 sections that flagged them. The previous review's complaint — "PLAN_P3 still describes the un-fixed shape as the design" — is answered.

---

## 4. Updated merge gate (post-3rd-pass)

The eight-item merge gate from pass 1 / pass 2 reduces to:

| # | Item | Status |
|---|---|---|
| 1 | §1 closed end-to-end (default_inv head-gated, wired into pverify) | ✓ |
| 2 | §A.1 closed (Proof-block-tag suffix) | ✓ |
| 3 | §A.3 closed (DefaultInvariants in pre AND post) | ✓ |
| 4 | §2.1 / §2.6 deferrals documented in PLAN_P3 | ✓ |
| 5 | Phase3Errors.lean lands | ✓ |
| 6 | Phase2PingPong rewritten to #pverify | ✓ (trivial-invariant version; PongAfterPing waits on R15) |
| 7 | ObligationShape.lean lands | ✓ |
| 8 | M3 benchmark verifies end-to-end | ☐ (gated on R15) |

7/8 merge-gate items closed. Item 8 is R15, which is real engineering work but distinct from the obligation-generator infrastructure this review covered. The recommendation:

**Phase 3a is done.** Flip Phase 3 to ◐→☑(a) in STATUS with the qualifier "Phase 3a: infrastructure + auto-discharge for trivial handlers; M3 benchmarks pending R15". M3 is then a Phase 3b milestone gated solely on emitting `#derive_lifted_wp` per-accessor and `loomSpec` per-primitive — both well-scoped follow-ups, neither of them an obligation-generator concern.

---

## 5. Suggested 3a → 3b hand-off

When R15 work begins, keep these in mind:

1. **`#derive_lifted_wp` per accessor.** [`GenModule.lean::emitVarAccessors`](../PLean/Commands/GenModule.lean#L349-L373) emits `<v>_get` and `<v>_set` defs but no spec. The M1 hand-written analogue in `HandPingPong.lean` issues `#derive_lifted_wp` for `get`/`set` once per program; the per-accessor version per pmodule should follow the same `open PartialCorrectness DemonicChoice in #derive_lifted_wp ...` shape that `emitDerivedWP` already uses for the bare `get`/`set`. Drop in alongside the def emissions.

2. **`loomSpec` lemmas for the framework primitives.** `PLean.send` / `PLean.raise` / `PLean.goto` / `PLean.markReceived` / `PLean.announce` need `@[loomSpec]` lemmas characterising their state effects so `wpgen` doesn't fall through to `WPGen.default`. These are *one-off* — they live in [`PLean/Semantics/Primitives.lean`](../PLean/Semantics/Primitives.lean), not per-pmodule, because the primitives themselves are program-independent.

3. **`pverify_wp` step may need to be aware of the new specs.** [`Tactic.lean:177-181`](../PLean/Verify/Tactic.lean#L177-L181) currently does `wpgen <;> first | apply WPGen.default | skip`; once the per-primitive `loomSpec` lemmas land, the `WPGen.default` fallback should fire on *fewer* sub-goals. Worth a regression test that confirms the M2 `ePing_handler` triple discharges via `pverify` after the specs land — which converts `Phase2PingPong.lean` from "trivial invariant" to the headline `PongAfterPing` invariant.

4. **Item 1.13 caveat.** The four PLAN_P3 deviation callouts may *each* turn into a full-fix in 3b. If §2.6 (registry-aware `is`) and §2.1 (`_handler_wrapped`) land, the M3 benchmarks should be swept to use the macro form (`m is Node` instead of `Node_allocated m`) so D20 is exercised in user-facing code.

---

## 6. Items I'm not flagging anymore (vs pass 2)

Resolved in this sweep — no re-action needed:

- §1.1 / §1.2 / §1.3 / §1.4 (pass 1 blockers — fixed in pass-1 sweep, retained in pass-2 sweep)
- §A.1 (pass-2 sharpened — fixed)
- §A.3 (pass-2 sharpened — fixed)
- §2.4 (dead code — fixed)
- §2.6 (`simp` drift in `default_inv` — fixed)
- §2.3 (DispatcherContract live import — fixed)
- §B.2 (silent spec-machine skip — fixed)
- §B.3 (env-rollback caveat undocumented — fixed in doc)
- §C / §6.6 (Phase2PingPong rewrite — landed)
- §6.7 (ObligationShape.lean — landed)
- §5.1 ("build green" overstated — wording fixed)
- §5.2 (PLAN_P3 not updated to match — four deviation callouts added)
- §1 (`default_inv` not wired into `pverify` — head-gated and wired)

---

## References

- [`docs/PLAN_P3.md`](PLAN_P3.md) — D18–D28 with new "Phase 3 implementation deviation" callouts at D20, D22, D27, D28
- [`docs/STATUS.md`](STATUS.md) — second-pass follow-up sweep entry summarises this pass's fixes
- [`docs/REVIEW_P3.md`](REVIEW_P3.md) — first / second pass (this file in its previous state)
- [`PLean/Verify/Tactic.lean`](../PLean/Verify/Tactic.lean), [`Obligation.lean`](../PLean/Verify/Obligation.lean), [`DispatcherContract.lean`](../PLean/Verify/DispatcherContract.lean) (inert)
- [`PLean/Internal/Registry.lean`](../PLean/Internal/Registry.lean), [`Surface/Verify.lean`](../PLean/Surface/Verify.lean)
- [`PLean/Commands/GenModule.lean`](../PLean/Commands/GenModule.lean)
- [`Tests/Surface/Phase3Errors.lean`](../Tests/Surface/Phase3Errors.lean), [`ObligationShape.lean`](../Tests/Surface/ObligationShape.lean), [`Phase2PingPong.lean`](../Tests/Surface/Phase2PingPong.lean), [`Phase2PingPong_manual.lean`](../Tests/Surface/Phase2PingPong_manual.lean), [`Phase3*.lean`](../Tests/Surface/), [`PVerifyTactic.lean`](../Tests/Surface/PVerifyTactic.lean)
- [`PLean.lean`](../PLean.lean) — DispatcherContract intentionally not re-exported
