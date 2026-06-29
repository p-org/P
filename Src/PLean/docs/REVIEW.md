# REVIEW — PLean

## TL;DR

- **Load-bearing strengths**: type-checked `@[pverifyProof]` shim (`Obligation.lean:196-204`, `:359-384`) plus `hasSorry` sweep through the user theorem (`Obligation.lean:155-164`) is the soundness backbone — every example we ran respects it. The `GlobalState`-shadow guard (`Verify.lean:317-326`), unconditional `injectKindGuards` over `system`-block invariants (`Verify.lean:498-562`), and the `<ev>_payload_of` sealing pattern (`GenModule.lean:494-513`) close the obvious cheating surfaces. The simp set (`SimpLemmas.lean`, `Containers.lean:119-203`) is entirely definitional / `propext`-grounded. Counter-example rendering is genuinely useful and the all-pass closure rates (DistributedLock 12/12, LockServer 37/37, RingLeader 14/14, ShardedKV 11/11, ClockBound 59/59, Consensus 16/16) demonstrate the pipeline works.
- **Top 3 soundness risks**: (1) `@[pverifyProof]` only checks `hasSorry` — a user can close any obligation with `Loom.SMT.trust_smt _` or a `False`-producing `paxiom`, and the registry will report `userProved` (`Obligation.lean:149-168`, `ProofRegistry.lean:11-12`); (2) `paxiom` does not run `rejectStateShadowIn` (`Verify.lean:806-812`), so `paxiom evil : ∀ s : GlobalState Sig, False` compiles and gets injected into every VC; (3) silent last-write-wins cross-file merge in the registry (`Registry.lean:77-105`) can drop a conflicting `event`/`invariant`/`Theorem` declaration with no warning.
- **Top usability friction**: ubiquitous `system <s> { … }` wrapper that every author writes identically; manual-proof boilerplate (12+ lines of `unfold`/`pverify_step_wp`/`rename_i` per obligation); the auto-default loop limitation that surfaces as a counter-example with no in-source pointer to the `triple_pforeach_with` workaround; no progress feedback during long `#pverify` runs.
- **Documentation gap**: `pverify.failOnIncomplete` is undocumented across PLAN/ROADMAP, and the live `set_option pverify.failOnIncomplete false` in `Examples/Consensus.lean:340` would mask future regressions in that file.
- **Code-style debt**: four files exceed 900 lines (`Tactic.lean` 1326, `Obligation.lean` 1254, `GenModule.lean` 1226, `Verify.lean` 946); `GenModule.lean` is full of `## Step 4d″`-style headers that the project's own style memo forbids.

---

## 1. Soundness

### 1.1 VC generation completeness (which paths get a VC)

The `synthesise` walker (`Obligation.lean:1099-1251`) covers `(machine, state, event)` triples from `sd.handles`, plus entry handlers from `machineEntryHandlers`, plus a per-invariant base case from each `Proof` block, plus a `prove default;` sweep that fills every `(M, S, ev)` and `(M, S, entry)` not user-directed (`Obligation.lean:1217-1245`). Handlers for `on ev goto tgt` are emitted by `materialiseStateBodyItem` (`GenModule.lean:812-840`) and appear in `sd.handles`, so goto-only states are correctly walked.

Holes:

- **COMPLETENESS GAP — Spec machines silently skipped.** `Obligation.lean:1169-1171` and `:1221` both `continue` on `m.isSpec`. The user-directive leg emits a `logInfo` once per `(Proof × spec machine)`; the auto-default leg is silent. Because spec syntax (Phase 4) isn't shipped, this is dormant — but the moment a spec machine can be authored, any `Theorem` whose VC ought to traverse a spec handler will silently report "all passed" with zero spec coverage. Promote the log to `logWarning`, dedupe across passes, and pin a test that fails the build if `m.isSpec` is ever true in the audited registry.
- **COMPLETENESS GAP — `defer` events.** The walker iterates `sd.handles`; whether deferred events are stored there (with a synthesised no-op handler) or in a parallel field is not visible from `Obligation.lean` alone. If they're dropped, deferred events get no VC — sound iff `defer` is a true semantic no-op, which the model needs to confirm.
- **COMPLETENESS GAP — `pnew M, arg` drops the init payload.** `Stmt.lean:144-151` discards `$_arg`; if a user expects an initial-state field carried by the constructor, the VC sees an arbitrary post-allocation kind with default fields. Sound (the obligation will fail to close if the proof depends on the arg) but counter-intuitive.
- **COMPLETENESS GAP — Mathlib-form multi-binder quantifiers.** `expandMultiBinder` (`Verify.lean:411-436`) only handles Lean's primitive `Term.forall`/`Term.exists`. The patterns in `injectKindGuards` (`Verify.lean:537-556`) match by Syntax kind suffix `endsWith ".exists"` (a fragile string check). If a user writes `∀ n m : Node, P` in a Mathlib-rebound notation, the kind guard may not be injected and the obligation runs against a bare `MachineRef`-typed binder.
- **COMPLETENESS GAP — loop-aware default invariants.** Confirmed in `Loop.lean:109-124` and CLAUDE.md: the auto-default chain has no `default_inv` that sees through `forWithInvariantLoop`. `Consensus.lean:312-335` works around with a hand-applied `triple_pforeach_with` (a proved lemma — sound), but the framework leaves a counter-example with no in-source breadcrumb pointing to the recipe.

### 1.2 Unsound verifier components / proofs slipped through

- **CRITICAL SOUNDNESS — `@[pverifyProof]` does not audit axioms.** `classifyOneObligation` (`Obligation.lean:149-168`) only checks `hasSorry`. A user-supplied proof of the form `exact Loom.SMT.trust_smt _` (the same axiom `loom_smt` legitimately uses on `unsat`) type-checks against the `_check` shim and is reported `userProved`. Similarly, `exact False.elim my_paxiom_of_False` closes any obligation when `my_paxiom : False` is declared via `paxiom`. The fix is a `CollectAxioms.collect` check at classification time with a strict allowlist (`propext`, `Classical.choice`, `Quot.sound`); `Loom.SMT.trust_smt` must **not** be allowlisted for manual proofs, and registry-introduced axioms should be reported in a "this VC depends on user axioms: …" footer.
- **UNSOUND-IF-MISUSED — `paxiom` skips `rejectStateShadowIn`.** `materialiseAxiom` (`Verify.lean:806-812`) accepts any prop. `paxiom contra : False` or `paxiom evil : ∀ s : GlobalState Sig, P s` is admitted and injected into every VC's lctx (`have hax_<n> := @<n>`). Compare `init-holds`, which does run the shadow rejection (`GenModule.lean:965`). Same asymmetry applies to `pinstance` (`Verify.lean:852-944`).
- **UNSOUND-IF-MISUSED — User-extensible `@[pverifySimp]`.** The attribute is not namespace-restricted. A user can register `theorem evil_simp : … := by sorry` and the obligation prep (`pverify_smt_prep` → `simp only [pverifySimp] at *`) would apply the bogus rewrite. The sorried lemma is detectable but not detected. Restrict registration to `PLean.*` or at minimum scan the active set for `hasSorry`.
- **UNSOUND-IF-MISUSED — `MachineRef = Nat` is a reducible abbrev** (`Label.lean:38`). Bare `∀ r : MachineRef, …` (or `∀ r : Nat, …` that projects from a label) escapes kind-guard injection because no kind is known. The wrapper-typed surface (`∀ n : Server`) is fine; the abbrev escape is a quiet footgun. Either gate `MachineRef` behind a structure (rejected upstream for runtime-flatness) or inject a "kind = 0 excluded" guard whenever a `MachineRef`-typed quantifier projects through `s.machines`.
- **UNSOUND-IF-MISUSED — `markReceived` is a public `@[reducible] def`** (`Primitives.lean:69-71`). A user handler body can call it on a forged label, breaking `ReceivedSubsetSent`. Currently *detected* by the auto-default VC (the post-state breaks `DefaultInvariants` and SMT returns counter-example), but it should be moved into a `PLean.Internal` namespace with `private` access and dispatched only by the runtime / obligation framework.
- **UNSOUND-IF-MISUSED — `choose bound` with negative `bound`.** `Primitives.lean:88-89` calls `pickSuchThat (fun x => 0 ≤ x ∧ x ≤ bound)`. When `bound < 0` the predicate set is empty; demonic-choice partial-correctness then makes `WP post = ⊤`, which lets a `choose` "verify" any postcondition. The fix is a one-line clamp at `def choose` or a precondition `bound ≥ 0` in the loomSpec.

### 1.3 SMT pipeline soundness (translation, prep, trust_smt)

The simp inventory in `Verify/SimpLemmas.lean` and `Semantics/Containers.lean:119-203` is clean — every entry is `propext`-grounded or a reducible `def` unfolding, conditional lookup lemmas are guarded by their `k' ≠ k` hypothesis, and no entry can rewrite a hypothesis to `True` falsely. The `pverify_smt_prep` chain (`Tactic.lean:472-488`) uses `simp only` + `dsimp only` + `unfold` + `try unfold` — definitional throughout. `abstract_machine_lookups` (`Tactic.lean:421-455`) is a `generalize`, which only weakens.

Trust anchor surfaces:

- **`Loom.SMT.trust_smt` axiom** (`Loom/SMT.lean:215`) is invoked on `.Unsat` (`SMT.lean:249`), on a cache hit in `pverifySmtCloseDefault` (`Tactic.lean:655`), and on the profiled variants (`:701`, `:746`). All paths gate on either a real solver `unsat` or a cache entry written only after a real `unsat`.
- **UNSOUND-IF-MISUSED — Cache key is a non-cryptographic hash without content re-check.** `pverifyHash` is `String.hash` over `Expr.toString`-pretty (`Tactic.lean:530-531`); on hit, the tactic asserts `trust_smt goalType` without reading the `.ok` file back and comparing to the current cache text (`Tactic.lean:655`). A 64-bit collision closes the unrelated goal vacuously. Realistic exploitability is low (cache dir is per-project `.lake/build/`), but the fix is one extra read-and-compare. Same caveat on the profiled path.
- **UNSOUND-IF-MISUSED — Solver-binary trust.** `Loom/SMT.lean:134-136` resolves `z3`/`cvc5` from `currentDirectory!`; a project-local malicious binary lying about `unsat` cannot be distinguished from a real one.

### 1.4 Soundness regression coverage — what's pinned, what isn't

Per project memory: two `SoundnessRegression` probes pin (a) the `GlobalState`-shadow guard on all binder shapes and (b) sorried `@[pverifyProof]` failing the build (probe 6). Both are verified in `Obligation.lean:155-164` and `Verify.lean:317-326`.

Not pinned:

- A `@[pverifyProof]` that proves the obligation via `Loom.SMT.trust_smt _` directly.
- A `paxiom contra : False` admitting `False` into every VC.
- A `paxiom evil : ∀ s : GlobalState Sig, P s` (state-binding `paxiom`).
- A user-registered `@[pverifySimp]` rewriting to `False`.
- A spec-machine present in the registry (currently structurally impossible, but the day Phase 4 lands the `continue` becomes a hole).
- Cross-file merge silently dropping a conflicting declaration.

These are the highest-priority new probes.

---

## 2. Side-stepping ('promised X but…')

### 2.1 Loop default invariants — promised but not auto-discharged

`foreach`/`while` syntax shipped, and `triple_pforeach`/`triple_pforeach_with` (`Loop.lean:70-124`) are proven meta-lemmas. The framework's auto-default chain, however, does not wire `triple_pforeach_with` into `pverify_default`. CLAUDE.md and ROADMAP both acknowledge this explicitly. Users running `prove default` on a loop-bearing handler see `[counter-example]` and must either fold `DefaultInvariants`-strength clauses into their loop invariant or write a manual `@[pverifyProof]` invoking `triple_pforeach_with`. Sound, but the most obvious "promised but the user has to solve it" gap in the codebase. The Consensus port (`Consensus.lean:312-335`) is the canonical workaround.

### 2.2 Manual proofs as escape hatches (when warranted vs when sketchy)

**Warranted**: LockServer's three manual proofs (`LockServer.lean:199-464`) and RingLeader's three (`RingLeader.lean:150-432`) work around lean-auto's higher-order rejection under `∀ … (s.machines _).currentState`, which is a translator limitation, not a logical one. Consensus's two `S.noConfusion` base-case proofs (`Consensus.lean:187-220`-ish) are init-state structural arguments where SMT brings no value.

**Sketchy**: Consensus's `entry_correct_block1_default` (`Consensus.lean:312-335`) is sound (uses `triple_pforeach_with`) but exists only because the framework can't generate the obligation through the loop. From an author's perspective this is the framework making a hard case the user's problem (see 2.1).

**Footgun**: `PingPongManual.lean:33-105` hand-writes a `theorem … _correct` whose shape matches the auto-emitted obligation but **does not register it** via `@[pverifyProof]` and **does not call `#pverify`**. A reader could mistake this for verification. Either add `#pverify` to the file or rename it to make the demonstrational nature obvious.

### 2.3 Axiom appeals (paxiom / pinstance) — protocol facts vs hidden assumptions

`RingLeader.lean` uses `pinstance order : LeOrder` and `pinstance ring : RingTopology` — clean: the axioms are total-order and ring-topology facts independent of the protocol; the typeclass declarations self-document them.

`Consensus.lean:169-173` is borderline: `axiom unique_quorum : ∀ s n1 n2, isQuorum n1 → isQuorum n2 → stateOf n1 s = Won_st → stateOf n2 s = Won_st → n1 = n2`. `isQuorum` is an opaque `function`; the axiom asserts "at most one machine satisfies `isQuorum ∧ Won` simultaneously" — which is essentially the safety conclusion routed through `isQuorum`. Reasonable as a Paxos-style assumption, but the example never proves `isQuorum` reflects a real majority, so the deployment-time gap is invisible to a reader. Flag the asymmetry vs `init-holds` (which carries a deployment caveat by name) — `paxiom` of this shape deserves the same.

`ShardedKV.lean:68-70` declares `unique_owner` as both a `Theorem` and an `init-holds` clause. Sound (the inductive step proves preservation), but a reader skimming "Theorem Safety" without reading the `init-holds` clause is misled. Adding `(deployment-assumed)` to the rendered output for invariants that are also `init-holds`-asserted would help.

### 2.4 Spec machines (Phase 4) — unwired with what guard?

`Obligation.lean:1169-1171` and `:1221` `continue`. The only user-visible signal is one `logInfo` per `(Proof × spec machine)` in pass 1; the auto-default pass is silent. There is no assert "no spec machine is present in the registry" — i.e., no structural lock preventing the day spec syntax lands from accidentally going green. Lift the log to `logWarning`, emit unconditionally per spec machine, and add a regression that the obligation count includes every `(spec-M, S, ev)` triple.

### 2.5 Entry handler precondition — looseness

`Obligation.lean:388-407, 442-445`: the pre is `Inv ∧ is_<M> this.ref s ∧ currentState = <S>_st` — *not* "fresh entry from a transition". This is **conservative**: the proof must handle entry from any pre-image that already has `currentState = <S>_st`, which is strictly stronger than the operational reality. Sound in the soundness direction; the cost is completeness for invariants that genuinely depend on entry-firing-once semantics. The docstring hints at future tightening (`stage = true`); if that lands, the cache must be invalidated to prevent retroactive validation of proofs that only worked under the loose pre.

### 2.6 Anything else where the framework made the hard case the user's problem

- `function f : T` (no body) emits `opaque` (`Verify.lean:820`), pushing responsibility for stating properties about `f` entirely onto user `paxiom`s. No `function`-level invariant story.
- `pnew M, arg` drops `arg` (`Stmt.lean:144-151`). Either model the constructor argument or reject the surface form.
- `using *` ("all preceding lemmas in this `Proof` block") is missing. `ClockBound.lean`'s 10 `prove … using …;` chains are mechanical and brittle.

---

## 3. Usability improvements

### 3.1 Error messages and diagnostics

Most user-facing errors are actionable (`Verify.lean:283-290`, `:332-339`, `:346-353`; `Tactic.lean:868-872`). The weak spots:

- **`Tactic.lean:109`**: "the `pverify` tactic chain did not close the goal" — accurate but doesn't tell the user the next move (e.g., "register a `@[pverifyProof]` with this signature: …").
- **`Verify.lean:100, 241, 848, 865`**: `"unrecognised lemma body item"` / `"unrecognised state body item"` should list the accepted shapes.
- **`Obligation.lean:1170`**: `logInfo` for spec-machine skip should be `logWarning`.
- **Truncation ceilings** in diagnostic output (12 lines / 1500 chars for tactic, `Obligation.lean:862`) are silent; print a footer "(output truncated; raise `pverify.diag.maxLines` to see more)".
- **Degraded manual-proof skeleton** when `ppSignature` throws (`PVerify.lean:63-64`) — degrades to name-only `theorem <thmName> := by sorry` which won't elaborate. Warn the user when this fallback fires.

### 3.2 Manual-proof affordances

Manual proofs across LockServer, RingLeader, and Consensus repeat the same boilerplate: dispatcher header (12 lines), `unfold` chain, `pverify_step_wp; intro s; intros; rename_i …` (positional and brittle), and the new-label-vs-old-label `by_cases hee : e = ⟨…, s.actionCount⟩; subst hee; injection hacte; simp only [Label.targets?]; subst htgte` pattern that appears six times in RingLeader's `block0_lemmas` alone.

Concrete suggestions:

- A `pverify_obligation_enter` tactic that introduces dispatcher hypotheses by **name** (registry-driven: `hInfl`, `hTgt`, `hKind`, `hSt`, `hAct`, and `hInv_<name>` for each preceding invariant), eliminating `rename_i` games.
- A `pverify_new_label_case_split` tactic that performs the dispatch case-split and hands back `new` / `old` named subgoals.
- An automatic skeleton dump in the failure report when an obligation could not be discharged — the user pastes it, fills the body, done.

### 3.3 Invariant authoring

Per the Examples audit, the strengthening-invariant burden is large: LockServer is 18 invariants for one safety claim; ClockBound 18 for three. Concrete proposals:

- **Drop `system <s> { … }` as a default.** Every Theorem in every example wraps in the same `system s { … }` with the same `s` and references `s` only via auto-injected guards. Make the wrapper inferred and surface the parameter only when the user opts in.
- **Routing sugar**: `invariant lock_to_server : never inflight eLock at Node` for the recurring "no `<ev>` is in flight to a `<kind>`" pattern.
- **`init-holds { … ; … }` block** so authors can group deployment assumptions instead of scattering top-level statements.
- **`derived` invariants**: `invariant unique_holder … derived for eGrant, eUnlock` to autogenerate the mechanical `no_lock_while_<ev>` strengthenings.
- **`using *`** in `Proof` blocks to drop the manual dependency chain in ClockBound and Consensus.

### 3.4 Configuration and defaults

- **`pverify.failOnIncomplete`** is undocumented in PLAN/ROADMAP/PLAN_P3/PLAN_P4. Document it and the CI risk (warning-only on `false`). The live `set_option pverify.failOnIncomplete false` in `Examples/Consensus.lean:340` is currently inert but would mask future regressions — remove or replace with a comment.
- **Inherited Loom options** (`loom.solver`, `loom.solver.smt.timeout`, `loom.solver.smt.retryOnUnknown`) are referenced in `pverifySmtCloseProfiled` but not documented anywhere in `PLean/`. Add a one-line block in `Tactic.lean` and the user docs.
- **`pverify.profile`** has a real semantic divergence ("not bit-identical to upstream `loom_smt`") that's buried in the docstring; promote to a one-line warning.

### 3.5 Performance / progress visibility

With `Elab.async = true`, `#pverify` runs concurrently and `runEmitOnly` scrubs per-obligation "Goal proven by"/"Trusting SMT solver" messages. The user gets no feedback until the entire command finishes — minutes for ClockBound. Recommendations:

- A `pverify.verbose : Bool` option that emits one line per obligation as it closes.
- A periodic heartbeat (`pverify.heartbeatMs`) for long obligations.
- Profile-mode output should be on by default at `info` severity, with the current `pverify.profile` controlling the per-stage breakdown.

---

## 4. Code style improvements

### 4.1 File-level organisation (too-large modules, suggested splits)

Four files exceed the project's ~500-line guideline:

- **`Tactic.lean` (1326 lines)**: split along section boundaries into `Tactic/Diag.lean` (50-115), `Tactic/Prep.lean` (118-490), `Tactic/Cache.lean` (490-595), `Tactic/Smt.lean` (596-770), `Tactic/Helpers.lean` (951-1221), `Tactic/Close.lean` (1223+).
- **`Obligation.lean` (1254 lines)**: split into `Obligation/Emit.lean` (170-636), `Obligation/Classify.lean` (842-1027), `Obligation/Walk.lean` (1099-1251).
- **`GenModule.lean` (1226 lines)**: hoist each `emitX` into `Commands/GenModule/<Topic>.lean`.
- **`Verify.lean` (946 lines)**: hoist `injectKindGuards` + `expandMultiBinder` + `rewriteFieldProjections` + `rejectStateShadowIn` into `Syntax/Verify/Hygiene.lean`.
- **`CexModel.lean` (744 lines)**: split into `CexModel/{NameCtx, Sexp, Decode, Render}.lean`.

### 4.2 Comment hygiene

The project memo forbids paths, plan-doc section IDs, phase numbers, decision narration, and dated commentary in source. Violations:

- **`GenModule.lean`** is full of `## Step 1`, `## Step 1b`, `## Step 4d″`, `## Step 5b`, `## Step 7d`, `## Step 4d′`. Rename to topic headings: `## Wrapper structs`, `## Union types`, `## Payload extractor characterisation`.
- **`Tactic.lean:558-561`** contains both a date ("profiled 2026-06-19") and a path reference (`Tests/Verify/ProfileProbe.lean`). Rewrite to describe the current representation only.
- **"the 2026-06-10 soundness fix"** appears in multiple files (`Verify.lean`, `Obligation.lean`). The fix is load-bearing; drop the date and describe the rationale.
- **`Tests/Syntax/SoundnessRegression.lean`** is referenced inline as a path — describe it by name only.

Comments that earn their keep: `GenModule.lean:142-156` (why kind-state coupling matters), `Obligation.lean:357-358` (why `pverify_log_failure_else_sorry` closes with sorry), `Tactic.lean:553-557` (why `Expr.toString` over `ppExpr` for cache).

### 4.3 Naming consistency

- Mixed `pverify_*` (macros) vs `pverifyX` (defs: `pverifyHash`, `pverifyCacheHas`, `pverifyCachePath`). Move helpers under a `PLean.Cache` sub-namespace.
- `emitX` (GenModule) vs `processXEmit` / `emitOneObligation` / `runEmitOnly` (Obligation) — distinct conventions across files for the same concept.
- `RefKinds` is an `Array`, not a `Map`; `kindOf` is linear-scan. Naming oversells the structure.
- `PProveDirective.isDefault` is fully derived from `target == \`default`; either drop or replace `target : Name` with `inductive Target = default | named (n : Name)`.
- `PProofDecl.name = Name.anonymous` as "anonymous" sentinel — use `Option Name`.

### 4.4 Function length / duplication

- **`emitOneObligation` (~200 lines, `Obligation.lean:183-386`) and `emitEntryObligation` (~150 lines, `:388-537`) share ~75% of their bodies.** Factor `buildHandlerCall`, `buildPre`, `buildPost`, `buildUnfoldChain`, `buildSpecHaves`, `buildAxiomHaves`, `wrapWithDiag`, `emitTheorem`.
- **`synthesise` (`:1099-1251`)** is three phases (user pass / auto-default pass / classify pass) inlined; lift each into a helper.
- **`emitProgramUnions` (`GenModule.lean:286-403`)** repeats the `for mname in ctx.machineOrder do … for v in vars do …` pattern at least 4×. Factor a `forEachMachineVar` iterator.
- **`machineFields` / `machineContainerFields` / `machineSetPropFields`** at `elabPGenModule:1043-1071` are three near-identical folds; a `partitionVars : VarInfo → 3-way` helper collapses them.
- **`sdestruct_state` and `destruct_machine_state`** (`Tactic.lean`) repeat the lctx-walk-and-destructure pattern; a generic `destructFirstOfType :: Name → TacticM Unit` would unify them.

### 4.5 Public/private boundaries

- **`Tactic.lean`**: `pverifyHash`, `pverifyCachePath`, `pverifyCacheHas`, `pverifySmtCloseDefault`, `pverifySmtCloseProfiled` are all `def` (public). Users don't call these. Make `private` or move into a `PLean.Cache` namespace.
- **`Profile.lean`**: `stateRef`, `inFlightRowsRef`, `modifyRow`, `beginObligation`, `endObligation`, `reset` — all exported. Users can poison the global state. The `IO.Ref.modify` is also a read-then-write, not atomic CAS — the docstring's "concurrent obligations can't race" overstates the guarantee. Make them `private` and expose a small wrapper.
- **`CexParse.lean`**: `demangleSexp`, `extractModelText`, `isBoilerplateName` are internal helpers. Only `parseModel` needs export.
- **`CexModel.lean`**: `RefKinds` exported as `abbrev`; `RefKinds.kindOf` is `private` but the type itself is open.
- **`Obligation.lean`**: `ObligationOutcome.glyph` / `.tag` are `def` (public) but only used inside the file.

Additional structural items:

- **`Registry.lean:77-105` last-write-wins** is silent across files. Same-name `event`/`invariant`/`Theorem` declarations in two files under the same `pmodule` should hit a duplicate-check at merge time, mirroring intra-file `errIfDuplicate`.
- **`PMachineDecl.materialised : Bool` + retained `body : Array Syntax`** is two flags for one invariant. Convert to `inductive PMachineStage = pending (body : …) | materialised (body : …)`.
- **`PInstanceDecl.classRepr` / `typeRepr` as `String`** for `#print_pmodule`: drop the strings, render on demand from `defStx`.
- **`CexParse.lean` hardcoded markers** (`"the goal is false:"`, `is_`, `_payload_of`, `_st`, `k!`, `gsSent` etc.) should be `private def`s with a single source of truth, shared between `isInternalName` and `isBoilerplateName` (currently overlap with non-disjoint responsibilities).

---

## 5. Recommendations (prioritised)

1. **Add axiom-allowlist check to `@[pverifyProof]` classification** (`Obligation.lean:149-168`). Run `CollectAxioms.collect` on the user theorem; allow only `propext`, `Classical.choice`, `Quot.sound`. Explicitly disallow `Loom.SMT.trust_smt` in manual proofs and warn on pmodule-registered `paxiom`/`pinstance` axioms. First step: write a regression test with `theorem foo := Loom.SMT.trust_smt _` and confirm it currently passes, then implement the check until it fails.
2. **Apply `rejectStateShadowIn` to `paxiom` bodies** (`Verify.lean:806-812`). Mirror the `init-holds` guard so `paxiom evil : ∀ s : GlobalState Sig, P s` is rejected at registration. First step: extend `materialiseAxiom` to call the existing helper.
3. **Cache content re-check on hit** (`Tactic.lean:655, 701, 746`). Read the `.ok` file back and byte-compare to the current `pverifyGoalToCacheText` before assigning `trust_smt`. Eliminates the 64-bit hash-collision surface for one extra file read per hit. First step: add the comparison; gate behind `pverify.cache.verify : Bool := true`.
4. **Promote spec-machine skip from `logInfo` to `logWarning`** and emit unconditionally per spec machine (`Obligation.lean:1170, :1221`). Add a registry-time assert that fails the build if a spec machine appears before Phase 4 ships. First step: change the log call and add a probe to `SoundnessRegression`.
5. **Cross-file merge duplicate detection** (`Registry.lean:77-105`). Reject same-key `event`/`invariant`/`Theorem` across files in the same `pmodule`. First step: in `mergeCtx`, replace the silent `insert` with `errIfDuplicate` for each map.
6. **Restrict `@[pverifySimp]` registration** to PLean-internal namespaces, or scan the active set for `hasSorry` before each `#pverify`. First step: add a `nsCheck` in the attribute handler.
7. **`pverify_obligation_enter` and `pverify_new_label_case_split` tactics** to collapse the 12-line manual-proof preamble and the repeated new-vs-old label case-split. First step: prototype `pverify_obligation_enter` in `Tactic/Helpers.lean` against LockServer's three proofs.
8. **Document `pverify.failOnIncomplete`, `pverify.profile`, and inherited `loom.solver.*` options** in a single block in `Tactic.lean` and `docs/`. Remove the live `set_option pverify.failOnIncomplete false` in `Examples/Consensus.lean:340`. First step: write the doc block; delete the option line.
9. **Wire `triple_pforeach_with` into `pverify_default`** so loop-bearing handlers can auto-discharge against `DefaultInvariants`. First step: add a `pverify_default_loop` close-chain branch that recognises `forWithInvariantLoop` in the goal and reduces via the proven lemma.
10. **Drop the `system <s> { … }` wrapper as a default**. Lower bare `Lemma`/`Theorem` bodies to `fun s => …` automatically; keep `system <s> { … }` for opt-in naming. First step: extend the materialiser to inject the lambda when the wrapper is absent, then port one example.
11. **Split `Tactic.lean`, `Obligation.lean`, `GenModule.lean`, `Verify.lean`, `CexModel.lean`** along their existing section boundaries. First step: `Tactic.lean` → `Tactic/{Diag,Prep,Cache,Smt,Helpers,Close}.lean` with no code change beyond moves.
12. **Strip phase-numbered headers and dated narration from comments** per the project memo. First step: `git grep -n "^-- ## Step\|2026-06-\|Tests/[A-Z]"` in `Src/PLean/PLean/` and rewrite each hit.
13. **Manual-proof skeleton in the failure report** when an obligation fails — render the obligation signature plus a `@[pverifyProof]` stub. First step: hook `renderSignature` into `renderDiagnostic` for `.unfinished` / `.disproved` / `.unknown` outcomes.
14. **`pverify.verbose` for progress feedback** during long runs. First step: add the option and emit a single line per obligation at end of `processOneEmit`.
15. **Clamp `choose bound` at 0** (`Primitives.lean:88-89`) to remove the vacuous-WP escape on negative bounds. First step: rewrite `def choose` to `pickSuchThat (fun x => 0 ≤ x ∧ x ≤ max 0 bound)` and re-verify ClockBound.
