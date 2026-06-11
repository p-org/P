# PLean — Phase 4 (Spec machines + residual P3) Plan

This document covers two intertwined work streams:

1. **The Phase-4 deliverable proper**: spec machines (`spec X observes
   [evs] { ... }`) get flattened to global vars + handler procedures
   and hooked into every `send` of an observed event. Mirrors
   PVerifier's [`SpecVariableDeclarations` /
   `GenerateSpecProcedures` /
   `GenerateSpecHandlers`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L980-L1088)
   and the [send-stmt hookup](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L1981-L1999).
   Target benchmark:
   [`Tutorial/Advanced/1_ChainReplicationVerification`](../../../Tutorial/Advanced/1_ChainReplicationVerification/PSrc/System.p) —
   the only Tutorial/Advanced benchmark with a `spec` block.

2. **Phase-3 residue**: items PLAN_P3 promised but Phase 3 deferred or
   shipped only partially. STATUS.md's "Deferred from REVIEW_P3" and
   "Open follow-ups (R15 — still gating M3)" subsections list these;
   they're collected here so a Phase-3-then-4 reader sees the full
   debt in one place.

Phase 4 ends with **M4**: from one `pmodule M` declaration containing
one or more `spec X observes [...] { ... }` blocks, `#gen_module`
emits per-spec global state + flattened handler defs, every `send`
of an observed event additionally fires the matching spec handler,
and `#pverify` synthesises both per-handler obligations *and*
spec-correctness obligations. The acceptance benchmark is
ChainReplication's `StrongConsistency` spec.

> **Read this first.** Phase 3 (currently ◐) shipped the obligation
> generator, the `pverify` tactic MVP, and the `Lemma`/`Theorem`/`Proof`
> surface but did *not* close any Tutorial/Advanced benchmark
> end-to-end. M3 acceptance therefore remains gated on R15 (per-
> accessor `#derive_lifted_wp`, per-primitive `loomSpec` lemmas).
> Phase 4 lands the spec-machine *infrastructure* but the M4 exit
> criterion ("ChainReplication verifies clean") inherits R15's
> blockage too — both R15 *and* `foreach`/maps (Phase 5) gate the
> headline ChainReplication invariants. We therefore split the M4
> exit into two parts: structural (parses + obligations emitted)
> and verified (closes end-to-end).

---

## Phase 3 audit (what carried over)

Re-checked against [`PLAN_P3.md`](PLAN_P3.md) §"Exit criterion" item-by-item.

### Done in code (Phase 3) — no follow-up

| # | Item | Where |
|---|---|---|
| D18 | Per-handler obligation shape | [`Obligation.lean::emitOneObligation`](../PLean/Verify/Obligation.lean) |
| D19 | `Lemma`/`Theorem`/`Proof` blocks | [`Surface/Verify.lean`](../PLean/Surface/Verify.lean), [`Internal/Decls.lean`](../PLean/Internal/Decls.lean), [`Internal/Registry.lean`](../PLean/Internal/Registry.lean) |
| D21 | `InitConditions` aggregation | [`GenModule.lean::emitInitConditions`](../PLean/Commands/GenModule.lean) |
| D23 | Obligation generator entry point | [`Obligation.lean::synthesise`](../PLean/Verify/Obligation.lean) |
| D25 | `using` clauses (preserves precondition strength + cycle detect) | [`Obligation.lean`](../PLean/Verify/Obligation.lean) (R17 mitigation) |

### Shipped, but deviates from plan — PLAN_P3 has implementation-deviation notes

| # | Plan-vs-code |
|---|---|
| D20 | `is` macro is **not registry-aware**. Emits `is_<rhs>` blindly; relies on Lean's name resolution. PLAN_P3 D20 has a deviation note. (REVIEW_P3 §2.6) |
| D22 | Tactic surface is `pverify` + `pverify_default` only. `pverify using L1, L2`, `pverify!`, `pverify?`, `loom_smt` SMT fallback all deferred. (REVIEW_P3 §2.5) |
| D27 | `_handler_wrapped` form **not built**. Obligations target `<M>.<S>.<ev>_handler` directly. Existential dispatcher contract carries the runtime guarantee. (REVIEW_P3 §2.1) |
| D28 | `default_inv` is head-symbol-gated `simp only + omega + rcases-lite`; the bounded mini-tactic case-table per primitive isn't built. (REVIEW_P3 §2.2) |

### Deferred — work to do

| # | Status | Owned by |
|---|---|---|
| D24 auto-emit `prove default;` per (M,S,ev) | ✗ user must write it explicitly | Phase 4 §"Residual P3 items" |
| `Verify/Wrapper.lean` (D27) | ✗ not in tree | Phase 4 §"Residual P3 items" / Phase 5 |
| `default_inv` full case-table (D28) | ✗ scaffold only | Phase 4 §"Residual P3 items" / Phase 5 |
| `pverify_solve` SMT fallback (D22 step 4) | ✗ ends at `tauto` | Phase 4 §"Residual P3 items" |
| Registry-aware `is` macro (D20) | ✗ name resolution only | Phase 4 §"Residual P3 items" |

### M3 acceptance benchmarks — none verified end-to-end

| Benchmark | Surface ports | `#pverify` closes |
|---|---|---|
| 6_DistributedLock | ☑ [`Phase3DistributedLock.lean`](../Tests/Surface/Phase3DistributedLock.lean) | ✗ commented out (R15) |
| 8_LockServer | ☑ [`Phase3LockServer.lean`](../Tests/Surface/Phase3LockServer.lean) | ✗ commented out (R15) |
| 3_RingLeaderVerification | ✗ not ported | — |

R15 (per-accessor `#derive_lifted_wp` + per-primitive `loomSpec`) is
the single biggest piece of work not done in Phase 3. It blocks every
benchmark beyond the no-`var`/trivial-handler case. Its resolution
is sub-task §"Residual P3 items" §1 below.

### Tests landed (Phase 3 ◐ partial-completion check)

| Test | Status | Note |
|---|---|---|
| Phase3PingPong (no-`var` end-to-end) | ☑ | `1 obligations from 1 prove-directives, all ✓` |
| ObligationShape (#guard_msgs-pinned) | ☑ | shape regression for the obligation generator |
| Phase3DuplicateTarget | ☑ | §A.1 disambiguation positive test |
| Phase3Errors (#guard_msgs error paths) | ☑ | covers §4.6 / §4.7 validations |
| Phase3R20 (kind ≠ 0) | ☑ | R20 mitigation pin |
| Phase2PingPong rewrite | ◐ | structurally `Theorem`/`Proof` shape; trivial invariant only — `PongAfterPing` lives in `_manual` |

---

## Phase-4 deliverable: spec machines

### What PVerifier does (the reference)

PVerifier's spec-machine flattening is a single emission pass split
across three procedures
([`Uclid5CodeGenerator.cs:980-1088`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L980-L1088)):

1. **`SpecVariableDeclarations`** (lines 980-993) — for each spec
   `S`, emit:
   - `type PSpec_<S>_StateAdt = enum { PSpec_<S>_<state₁>, ... };` —
     the spec's discrete control state, like the per-impl-machine
     state union `<Mod>.S` but with a `PSpec_` prefix.
   - `var PSpec_<S>_State : PSpec_<S>_StateAdt;` — global state
     variable.
   - For each spec field `f`: `var PSpec_<S>_<f> : <ty>;` — global
     field variable.

2. **`GenerateSpecProcedures`** (lines 995-1042) — for each spec
   method (entry / handler / inline `fun`), emit a UCLID5 `procedure`
   that copies global → local, runs the body in local-prefix mode
   (so `kv[req.k] = req.v` operates on local copies), then copies
   local → global at the end. Effectively a "snapshot, run, commit"
   pattern. This is exactly the encoding PVerifier needs to make
   `assert <prop>;` inside a spec handler verify against the
   *current* global state.

3. **`GenerateSpecHandlers`** (lines 1044-1088) — for each
   `(spec, observed event)` pair, emit a dispatch procedure
   `PSpec_<S>_<ev>` that pattern-matches on the spec's current
   state and calls the corresponding `procedure` from step 2.

4. **`SendStmt` patch** ([`Uclid5CodeGenerator.cs:1981-1999`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L1981-L1999)):
   after the existing `sent[lbl] := true; actionCount += 1;`
   sequence, emit `call PSpec_<S>_<ev>(<payload>);` for every spec
   `S` that observes `ev` (tracked in `_specListenMap`).

The whole thing is **synchronous within a handler step**: when a
handler `H` calls `send t, ev, p`, the spec procedure runs as the
*next* statement in `H`'s WP, *before* `H`'s WP descends into the
rest of `H`'s body. This is what makes spec invariants verify
against the right snapshot.

### Translating to PLean

Phase 4's emission has the same shape but lands in Lean:

- **Spec state** lives inside `<Mod>.GS` (the per-pmodule
  `GlobalState`), as additional record fields. Either we extend
  `GlobalState` with a `specs : SpecState` field (a per-pmodule
  bundle, parameterised over `Sig`), or we widen the existing
  `Fields` union to include spec fields keyed on a different
  ctor. The first is cleaner because it doesn't conflate
  per-machine-instance fields (indexed by `MachineRef`) with
  per-spec-singleton fields. **D29 (below) picks the design.**

- **Spec methods** become `PM`-monad defs in the generated module,
  shaped like impl handlers but with the `(this : <M>)` parameter
  replaced by access to the per-pmodule spec state field. The
  "snapshot / run / commit" pattern PVerifier emits is implicit in
  Lean's `StateT` semantics — we just operate on the spec's slice
  of `GlobalState` and `set` the updated whole.

- **`announce` and `send`-on-observed-event** trigger spec
  handlers. Phase 4 modifies [`Surface/Stmt.lean::send`](../PLean/Surface/Stmt.lean)
  (and `announce`) to additionally call any spec procedures that
  observe the event. The list of `(spec, event)` triggers comes
  from the registry (`PMachineDecl.observed`).

- **Spec correctness obligations** (D32 below): every
  `assert <prop>;` inside a spec handler becomes a per-handler
  triple obligation that says "if the precondition holds and the
  spec handler fires after a `send` of an observed event, the
  asserted prop holds." Discharged via the existing `pverify`
  pipeline.

### Why this is a good Phase

1. **Surface is small.** `spec X observes [evs] { ... }` is already
   parsed and registered (Phase 0). Phase 4 only needs to extend
   `#gen_module` to materialise spec bodies and patch `send` to
   trigger spec handlers.

2. **One non-trivial benchmark.** ChainReplication is the only
   Tutorial/Advanced benchmark with a spec, and its `StrongConsistency`
   spec is small (one state, two handlers, three asserts). The
   structural part of M4 should be ≤2 days; the verified part is
   gated on Phase 5 (the spec uses `map[int,int]` and `if (k in kv)`,
   neither of which Phase 3 supports).

3. **Reuses Phase-3 infrastructure.** The obligation generator
   (`Verify/Obligation.lean`) extends naturally to a `synthesiseSpec`
   pass; the `pverify` tactic doesn't change. The new code is
   localised to `Surface/Stmt.lean` (send-trigger), `Commands/GenModule.lean`
   (spec emission), and a new `Verify/SpecObligation.lean`.

---

## Confirmed design decisions (Phase 4)

These extend [`PLAN_P3.md` § "Confirmed design decisions"](PLAN_P3.md#confirmed-design-decisions-phase-3).
Numbering continues so `D29` here is `D29` in any cross-reference.

1. **D29 — Spec state lives in a separate `<Mod>.SpecState` record.**
   `ProgramSig` gains an `Specs : Type` field; `<Mod>.SpecState`
   is the record `{ <S₁>_state : <S₁>_StateTag; <S₁>_<f₁> : ...; ...; <S₂>_... }`
   covering every spec in the pmodule. `GlobalState` gains a `specs
   : P.Specs` field. Phase 0/1's existing `GlobalState` shape needs
   to grow this field — backward-compatible default to `default :
   Specs` (so M1/M2 hand-written examples and Phase-3 tests keep
   building).

   *Alternative considered*: widen `<Mod>.Fields` to include spec
   fields keyed on a synthetic spec-singleton `MachineRef`. Rejected
   because (a) a spec is *not* a machine instance — it's a global
   logic-level observer; (b) the `Fields` union is already keyed by
   machine kind, so mixing in spec fields complicates the
   `<v>_get`/`<v>_set` accessor emission.

2. **D30 — Spec procedures are `PM Sig Unit` defs over the
   `<Mod>.SpecState` slice.** Each spec handler `H` becomes
   `def <Mod>.<Spec>.<state>.<ev>_handler (param : <ev>_payload) :
   PM Sig Unit := do <body>` where `<body>` reads/writes
   `<Mod>.<Spec>` fields via fresh `<Spec>_<v>_get`/`_set` accessors
   (analogous to per-machine `<M>.<v>_get`/`_set` but indexed by
   the spec name, not a `MachineRef`).

   Like impl handlers, spec handlers don't take a `this` parameter;
   the spec is a singleton.

3. **D31 — Spec triggers fire as a sequence after `send`.** The
   `send` macro ([`Surface/Stmt.lean`](../PLean/Surface/Stmt.lean))
   is extended so `send target, ev, payload` emits:

   ```lean
   PLean.send (P := Sig) target (E.<ev> payload)
   <Mod>.<Spec₁>.dispatch_<ev> payload   -- for each Spec_i observing ev
   <Mod>.<Spec₂>.dispatch_<ev> payload
   ...
   ```

   The dispatch defs are emitted by `#gen_module` per
   `(spec, event)` pair and case-split on the spec's current
   `<Spec>_state` to pick the right handler. Mirrors PVerifier's
   `GenerateSpecHandlers` exactly.

   **Subtlety**: the sequence `send; dispatch;` is part of the
   *handler's* `do`-block, so its WP is computed by `wpgen` in the
   normal pipeline. No new `wpgen` step is needed; we just
   schedule the dispatch into the handler body.

4. **D32 — `assert <prop>;` becomes a per-spec-handler
   obligation.** Each `assert` inside a spec handler emits a
   side-obligation: `triple <pre> <pre-assert prefix>; <post>` where
   `<post> = <prop>`. The obligations land at:

   ```lean
   theorem <Mod>.<Spec>.<state>.<ev>_assert<n>_correct ...
   ```

   where `<n>` is the assert's positional index in the handler.
   Discharged via `pverify`.

   **Spec correctness vs. impl correctness** are now distinct
   obligation classes: impl obligations preserve user invariants;
   spec obligations discharge `assert`s. Both feed the same
   `#pverify M` report.

5. **D33 — `assume <prop>;`** (deferred to Phase 5; mentioned for
   completeness). When inside a spec body, an `assume` tightens
   the precondition for the surrounding handler obligation. Not in
   M4 scope; flagged here so PLAN_P5 can pick it up.

6. **D34 — Spec bodies allow `if (k in kv)` etc — but Phase 4 only
   accepts the *single-spec, single-state* shape.** The
   `StrongConsistency` spec in ChainReplication has one state
   (`WaitForEvents`) and uses `if (resp.k in kv)`. The `in`
   operator and `map[K,V]` access are Phase-5 territory; Phase 4
   parses them and emits a stub `pure ()` body for any handler
   that uses them, so the obligation infrastructure round-trips
   even when the user invariant body isn't yet decidable.

   This is the same staging Phase 3 used for `prove default
   using X` — the surface accepts the shape, the obligation
   generator emits the theorem, and `#pverify` reports the failure
   without aborting.

7. **D35 — Spec machines participate in `<Mod>.MKind`.** Phase 3
   already includes spec machines in `MKind` (per the comment in
   `Commands/GenModule.lean::emitMachineKinds`). Phase 4 makes that
   meaningful: a `forall (m : machine) :: m is StrongConsistency
   ==> ...` invariant body can reference the spec by name. This
   resolves a latent Phase-3 question (spec is in `MKind` but the
   spec has no instances, so `is_StrongConsistency m` is always
   false unless the user calls `pnew Spec`). Phase 4 needs to
   either (a) document that spec machines are conceptually
   "always allocated at index 0 of the spec table" — a different
   slot from `MachineRef`, since they're singletons, or (b) make
   `m is StrongConsistency` check a different (per-pmodule,
   compile-time) predicate that just inspects whether the spec
   exists. Decision: **(b)**, more in line with PVerifier's
   `_specListenMap`-driven semantics. The `is` macro gains a
   third branch for spec names.

---

## Module list (Phase 4 deliverables)

```
Src/PLean/PLean/
  Semantics/
    GlobalState.lean             # MODIFIED: ProgramSig.Specs : Type;
                                 # GlobalState.specs : P.Specs (D29).

  Surface/
    Stmt.lean                    # MODIFIED: `send` macro now also
                                 # emits dispatch calls for spec
                                 # observers (D31).

  Commands/
    GenModule.lean               # MODIFIED: emit per-spec state
                                 # record + spec-state ctor; emit
                                 # spec-handler defs (D30); emit
                                 # per-(spec, event) dispatch defs
                                 # (D31). Spec body materialisation
                                 # supports `assert <prop>;` —
                                 # registers the assert in a new
                                 # registry side-table.

  Verify/
    SpecObligation.lean          # NEW: synthesise per-(spec, state,
                                 # event, assert-index) obligations
                                 # (D32). Mirrors `Obligation.lean`'s
                                 # shape, with spec-state precondition
                                 # taking the place of the impl-machine
                                 # dispatcher contract.

  Internal/
    Decls.lean                   # MODIFIED: PMachineDecl gains
                                 # `assertSites : Array AssertSite`
                                 # for spec machines, populated at
                                 # registration time.
    Registry.lean                # MODIFIED: cross-file merge handles
                                 # the new field.

Src/PLean/Tests/Surface/
  Phase4SpecPingPong.lean        # NEW: M4-trivial. A pmodule with
                                 # one impl machine and one spec
                                 # observing the impl's events.
                                 # The spec asserts `True`. Confirms
                                 # the structural pipeline (spec
                                 # state, dispatch, `assert`
                                 # obligation) lands.
  Phase4ChainReplication.lean    # NEW: M4-acceptance. Port of
                                 # Tutorial/Advanced/1_ChainReplicationVerification
                                 # surface. `#pverify` produces
                                 # both impl obligations (R15-gated)
                                 # AND spec obligations (Phase-5
                                 # `map`/`in`-gated). Both
                                 # categories report failures via
                                 # the existing R19 capture path —
                                 # the test's success criterion is
                                 # "obligations emit cleanly", not
                                 # "obligations close".
  Phase4SpecObligationShape.lean # NEW: #guard_msgs-pinned spec
                                 # obligation theorem signatures
                                 # (mirrors Phase-3 ObligationShape).
```

---

## Phase 4 work breakdown (ordered)

### 1. **Residual P3 — auto-emit `prove default;` per (M, S, ev) (D24)** *(½ day)*

PLAN_P3 D24 promised `prove default` would be auto-emitted regardless
of whether the user wrote it. Phase 3 ships requiring the user to
write `prove default;`. Adding this to `synthesise` is a 5-line
loop: after walking `ctx.proofs`, walk every `(M, S, ev)` and emit
a default-only obligation. Idempotency: if the user already wrote
`prove default;`, the auto-emission would collide on theorem name —
the §A.1 disambiguation suffix avoids that (the user's gets
`<proofTag>_default` and the auto-emission can use `block_auto_default`
or similar).

Test: extend `Phase3PingPong.lean` (or a new Phase4 file) to
*not* write `prove default;` and confirm the obligation emerges
anyway. Catches D24 regressions.

Why this is in Phase 4: spec machines need this (`assert`
obligations are conceptually adjacent to default-invariant
obligations), and it's tiny.

### 2. **`ProgramSig.Specs` + `GlobalState.specs`** *(½ day)*

Extend [`Semantics/GlobalState.lean`](../PLean/Semantics/GlobalState.lean):

```lean
structure ProgramSig where
  E : Type
  G : Type
  S : Type
  F : Type
  Specs : Type   -- NEW
  ...
```

Add `Specs : Type` with default `Unit` so existing code
(M1, M2, Phase-3 tests) keeps building. `GlobalState` gains
`specs : P.Specs`; the `Inhabited` instance provides
`default : Specs`.

The default = `Unit` matters because every existing M1/M2 test
constructs `Sig := { E := ..., G := ..., S := ..., F := ... }`
without specifying `Specs`. Lean's record-default-value resolution
picks up the default. Confirm via the StackSpike + HandPingPong +
Phase3PingPong tests staying green.

Exit: M1/M2 still build; new test that exercises `s.specs`
type-checks.

### 3. **`#gen_module` emits per-spec state + handlers (D29 / D30)** *(1 day)*

Within the existing `#gen_module` pipeline, after `emitProgramUnions`
and before per-machine handler emission, run a new
`emitSpecMachinery` step:

1. For each `spec S { ... }` in `ctx.machineOrder` with `m.isSpec`:
   a. Emit `inductive <Mod>.<S>_StateTag | <s₁> | <s₂> | ...`
   b. Emit `structure <Mod>.<S>_Fields where <f₁> : <ty₁>; ...`
   c. Per-spec accessors `<S>.<v>_get` / `<S>.<v>_set` operating
      on `s.specs.<S>_fields.<v>`.

2. Materialise spec handler defs: each `on ev (param : T) { body }`
   inside spec `S` becomes
   `def <Mod>.<S>.<state>.<ev>_handler (param : T) : PM Sig Unit := do <body>`,
   where `<body>` is the user body with the same `var`-binding
   prologue impl handlers get.

3. Per-`(spec, event)` dispatch defs:
   ```lean
   def <Mod>.<S>.dispatch_<ev> (param : <ev>_payload) : PM Sig Unit := do
     let st ← <S>_state_get
     match st with
     | <state₁>_st => <S>.<state₁>.<ev>_handler param
     | <state₂>_st => pure ()  -- not handled in this state
     | ...
   ```

Spec machines participate in `<Mod>.MKind` (D35) so the `is`
macro can resolve `m is <Spec>` consistently.

Exit: a tiny test pmodule with one impl + one spec elaborates;
`#check @<Mod>.<S>.dispatch_<ev>` succeeds.

### 4. **`send` macro triggers spec dispatch (D31)** *(½ day)*

In [`Surface/Stmt.lean`](../PLean/Surface/Stmt.lean), extend the
`send` macro emission so after `PLean.send (P := Sig) target
(E.<ev> payload)` it runs every dispatch def for specs that
observe `<ev>`. The list comes from the registry at expansion
time — `getLocalPModuleCtx?` then walk `ctx.machines` for spec
machines whose `.observed` contains `<ev>`.

Implementation note: the dispatch is part of the *handler's*
`do`-block, so its WP gets computed by `wpgen`. We need
`#derive_lifted_wp`-style specs for the dispatch defs *and* the
underlying spec accessors, otherwise `wpgen` falls back to
`WPGen.default` and the spec obligation goal goes opaque (same
R15-flavour issue as impl accessors). For Phase 4 this means
emitting per-spec `#derive_lifted_wp` for `<S>_<v>_get`/`_set`
alongside the per-impl-machine ones — pre-empting the R15 work
for the spec slice.

Same applies to `announce` (which is already a `send` variant).

Exit: the M4-trivial test fires a spec handler from an impl
handler's `send`; the resulting `GlobalState` shows the spec
field has been updated (a `decide`-based regression on
`s.specs.StrongConsistency_kv` post-send).

### 5. **`assert` registration + spec-obligation generator (D32)** *(1 day)*

Extend [`Internal/Decls.lean`](../PLean/Internal/Decls.lean) to
record `assert` sites: a new `PAssertSite { specName, stateName,
eventName, assertIdx, propStx, ref }` array hung off
`PMachineDecl` (only for spec machines; empty for impls).

`#gen_module` collects assert sites at registration time by walking
spec handler bodies. (We can reuse the existing `collectSentEvents`
walker shape — match on a new `assert` syntax.)

`Verify/SpecObligation.lean` (new) provides
`synthesiseSpec : LocalPModuleCtx → CommandElabM Unit` that walks
every `(spec, state, event, assertIdx)` and emits:

```lean
theorem <Mod>.<Spec>.<state>.<ev>_assert<n>_correct
    (param : <ev>_payload) :
    triple (l := PProp Sig)
      <pre: dispatcher contract for spec, plus user-invariant pre>
      <prefix of handler body up to and including the assert>
      (fun _ s => <propStx s>) := by
  pverify
```

`#pverify M` calls `synthesiseSpec` after `synthesise` (for impl
obligations); both contribute to the report.

The "prefix of handler body up to the assert" is the trickiest
part: we need to emit a sub-def per `(spec, state, ev, n)` that
runs only the prelude. Implement by walking the saved spec-handler
body Syntax, splicing in everything up to (but not including) the
assert at index `n`.

Exit: M4-trivial obligation report includes one spec obligation
per `assert` (e.g., `... 3 obligations from 1 prove-directives,
all ✓; 2 spec assertions, all ✓`).

### 6. **`is m <Spec>` macro branch (D35)** *(¼ day)*

[`Surface/Notation.lean`](../PLean/Surface/Notation.lean)'s `is`
macro currently emits `is_<rhs> $lbl` and lets Lean resolve.
Spec machines get the same `is_<S>` alias treatment in
`emitMachineKinds`, but the meaning is different: a `MachineRef`
can never *be* a spec (specs aren't allocated). For Phase 4 we
emit `is_<Spec> _ s := False` (a vacuous predicate) so user
invariants like `forall (m : machine) :: m is StrongConsistency
==> ...` parse and discharge trivially.

Test: `Phase4SpecPingPong.lean` includes a `forall m, m is
StrongConsistency → False` invariant that closes via `decide`.

### 7. **M4-trivial test** *(½ day)*

[`Tests/Surface/Phase4SpecPingPong.lean`](../Tests/Surface/Phase4SpecPingPong.lean) (NEW):

```lean
pmodule Phase4SpecPingPong
  event eHello

  machine M {
    start state S {
      on eHello {
        send this.ref, eHello, ()  -- triggers spec dispatch
      }
    }
  }

  spec Watcher observes [eHello] {
    var count : Nat
    start state Watching {
      on eHello (_p : eHello_payload) {
        count = count + 1
        assert count >= 0  -- trivial; closes via decide / grind
      }
    }
  }

  Theorem trivial { invariant t : ∀ s, True }
  Proof Safety { prove trivial; }
end Phase4SpecPingPong

#gen_module Phase4SpecPingPong
#pverify    Phase4SpecPingPong
```

Expected output:
```
Phase4SpecPingPong: well-formed (1 events, 1 machines, 1 specs, 1 invariants)
Phase4SpecPingPong: 1 obligations from 1 prove-directives, all ✓
Phase4SpecPingPong: 1 spec assertions, all ✓
```

### 8. **M4-acceptance: ChainReplication structural port** *(1 day)*

[`Tests/Surface/Phase4ChainReplication.lean`](../Tests/Surface/Phase4ChainReplication.lean) (NEW)
ports the surface of [`Tutorial/Advanced/1_ChainReplicationVerification/PSrc/System.p`](../../../Tutorial/Advanced/1_ChainReplicationVerification/PSrc/System.p),
including the `spec StrongConsistency observes [eReadResponse,
eWriteResponse] { ... }` block.

`#pverify` runs and reports BOTH impl obligations (R15-gated; will
fail on the heavy `next_1`/`next_2`/`next_3` invariants) AND spec
obligations (Phase-5-gated; the `if (resp.k in kv)` body uses
`map`-membership which Phase 4 doesn't yet support — the spec
handler stub elaborates to `pure ()` per D34).

Exit: the file builds clean (warnings about incomplete
obligations, but no fatal errors); the obligation report names the
right number of impl + spec triples.

### 9. **`Phase4SpecObligationShape.lean`** *(¼ day)*

Mirrors Phase-3's `ObligationShape.lean`. `#guard_msgs`-pins one
representative spec obligation signature so future refactors of
`SpecObligation.lean` don't silently change emission shape.

### 10. **Stretch — `assert` / `assume` general-purpose** *(¼ day; can defer to P5)*

If `assert` outside a spec is ever needed, [`Surface/Stmt.lean`](../PLean/Surface/Stmt.lean)
should error with a clear message ("`assert` is only legal inside
spec machine bodies — use `invariant` for handler/global facts").
This pre-empts a confusing error at WP-step time.

`assume` is already partially specified (PLAN_P3 §"Hand-off to
Phase 4 and beyond" and our D33 stub); leave for Phase 5.

---

## Residual P3 items (work that lands in Phase 4 *if* we have time)

These are tracked in STATUS's "Deferred from REVIEW_P3" subsection.
Phase 4 owns them only if M4 finishes early; otherwise they slip
to Phase 5 (or are explicitly cancelled in PLAN.md).

### R-P3.1 — Per-accessor `#derive_lifted_wp` (R15) *(2 days)*

The single biggest blocker for any benchmark with `var`-bearing
handlers. `#gen_module::emitVarAccessors` currently emits
`<v>_get`/`<v>_set` defs; Phase 4 should additionally invoke
`#derive_lifted_wp` per accessor, similar to how the per-pmodule
`get`/`set` for the underlying `StateT` is derived in
`emitDerivedWP`. With that, `wpgen` steps through accessor reads
and writes and the M3 benchmarks become reachable.

**This is a Phase 4 priority because spec accessors need the
same treatment.** Doing both passes together is more efficient
than doing impl in Phase 4 and spec separately later.

Exit: re-enable `#pverify` in `Phase3DistributedLock.lean` and
have at least the `prove default;` obligations close.

### R-P3.2 — Per-primitive `loomSpec` lemmas (R15 cont'd) *(1 day)*

`PLean.send` / `PLean.raise` / `PLean.goto` / `PLean.markReceived`
/ `PLean.announce` need `@[loomSpec]`-tagged WP lemmas so `wpgen`
can step through them without `WPGen.default`. Each is ~10-15
lines (mirror Cashmere's primitive specs).

Combined with R-P3.1, the M3 benchmarks should at minimum have
`prove default;` discharging end-to-end.

### R-P3.3 — `_handler_wrapped` form (D27) *(½ day)*

If R-P3.1 + R-P3.2 land and the obligations still don't close
because of the existential dispatcher contract not unfolding,
implement `Verify/Wrapper.lean` per PLAN_P3 D27. The wrapper
makes `lbl` a real parameter, sidestepping the existential.
Otherwise leave as-is.

### R-P3.4 — Registry-aware `is` macro (D20) *(½ day)*

[`Surface/Notation.lean`](../PLean/Surface/Notation.lean)'s `is`
macro should consult `getLocalPModuleCtx?` and dispatch on
event-vs-machine-vs-spec-vs-unknown, with bespoke errors. PLAN_P3
D20 prescribed this; Phase 3 deferred. Phase 4 adds the spec
branch (D35), so doing the full registry dispatch at the same
time is natural.

### R-P3.5 — `default_inv` mini-tactic case-table (D28) *(1 day)*

PLAN_P3 D28 prescribed three named mini-tactics
(`unique_actions_step`, `increasing_count_step`,
`received_subset_sent_step`) with bounded leaf counts and case
tags. Phase 3 ships a `simp only + rcases-lite + omega` scaffold.
The full implementation matters once R-P3.1/.2 land and we hit
handlers with `markReceived + send` chains. Estimate ~150 lines
total.

### R-P3.6 — `pverify_solve` SMT fallback (D22 step 4) *(½ day)*

Add `loom_smt [hints]` after `tauto` in `pverify_solve`'s `first`
chain, gated by `loom.solver` option (default off). The SMT
roundtrip test ([`Tests/Semantics/SmtRoundtrip.lean`](../Tests/Semantics/SmtRoundtrip.lean))
shows `loom_smt` works for first-order goals over `Nat`/`Int`/`Bool`
but rejects `GlobalState` field-functions as higher-order. Phase 4
wires SMT for the goals where it works; the GlobalState-shaped
defunctionalisation pre-pass is a follow-up.

### R-P3.7 — `pverify using L1, L2`, `pverify!`, `pverify?` *(1 day; deferable)*

PLAN_P3 D22's listed variants. The `using` form duplicates the
obligation generator's preamble work, so it's mostly cosmetic;
`pverify!` and `pverify?` are debug aids. Defer if time-pressed.

---

## Module list summary

```
Phase 4 deliverables (M4 structural):
  Semantics/GlobalState.lean    [MODIFIED: ProgramSig.Specs, GlobalState.specs]
  Surface/Stmt.lean              [MODIFIED: send macro triggers spec dispatch]
  Surface/Notation.lean          [MODIFIED: is macro spec branch (D35)]
  Commands/GenModule.lean        [MODIFIED: emit spec state + handlers]
  Internal/Decls.lean            [MODIFIED: PAssertSite registry side-table]
  Internal/Registry.lean         [MODIFIED: cross-file merge for assert sites]
  Verify/SpecObligation.lean     [NEW]
  Tests/Surface/Phase4SpecPingPong.lean        [NEW: M4-trivial demo]
  Tests/Surface/Phase4ChainReplication.lean    [NEW: M4-acceptance structural]
  Tests/Surface/Phase4SpecObligationShape.lean [NEW: #guard_msgs pin]

Residual P3 (lands in Phase 4 if time permits, otherwise Phase 5):
  Commands/GenModule.lean        [MODIFIED: per-accessor #derive_lifted_wp]
  Semantics/Primitives.lean      [MODIFIED: @[loomSpec] lemmas per primitive]
  Verify/Wrapper.lean            [NEW: D27 _handler_wrapped form]
  Verify/Tactic.lean             [MODIFIED: full default_inv case-table; loom_smt fallback]
```

---

## Exit criterion (M4)

Phase 4 ends when:

1. **M4-trivial verifies clean.**
   `Tests/Surface/Phase4SpecPingPong.lean` builds and `#pverify`
   reports *both* the impl-side obligations AND the spec-side
   `assert` obligations, all ✓.

2. **M4-acceptance lands as a structural port.**
   `Tests/Surface/Phase4ChainReplication.lean` builds. `#pverify`
   produces the right number of impl + spec obligations
   (counts pinned via `#guard_msgs`). The obligations themselves
   do *not* need to close — both R15 (still gating impl
   obligations on benchmarks beyond DistributedLock) and Phase-5
   `map`/`in`/`foreach` (gating the heart of ChainReplication's
   spec body) are still future work.

3. **Phase-3 residue audit refreshed.**
   STATUS.md's "Open follow-ups" lists which R-P3.x items landed
   in Phase 4 vs. slipped to Phase 5.

4. **STATUS.md: Phase 4 → ◐ or ☑.**
   ☑ if both M4-trivial and M4-structural land; ◐ if only
   M4-trivial. Decision-log entries D29–D35.

---

## Risks / things to watch

Inherits PLAN_P3's residual list. New risks specific to Phase 4:

- **R22 — Spec dispatch in `send` changes the WP shape.**
  Inserting `<Spec>.dispatch_<ev>` calls after every `send`
  changes every existing handler obligation: the post-state now
  includes the spec's update too. Existing M3 tests should still
  close (the spec-state field is a new component of `GlobalState`
  that user invariants don't reference, so it doesn't affect
  goals over the user invariant). Worth pinning M3 obligations
  via `ObligationShape.lean` and confirming the shape diff is
  just the spec field's appearance.

- **R23 — Spec handler bodies that use `if (k in kv)` etc.**
  ChainReplication's spec uses `map[K,V]` membership. Phase 4
  emits `pure ()` for these (D34); the resulting `assert`
  obligations are vacuous. Worth a clear log message
  (`spec assertion @ Phase4ChainReplication.StrongConsistency.WaitForEvents.eReadResponse#1: handler body uses unsupported feature; obligation emitted with stub body`)
  so the user sees what's pending.

- **R24 — Specs in cross-file pmodule fragments.**
  Phase 0's `pmodule M ... end M` works across files, but a
  `spec X` declared in one file can be observed by an impl
  handler in another. The cross-file `pmoduleExt` merge already
  handles this for impl machines; verify it does for specs.
  Test by splitting M4-trivial into a multi-file form.

- **R25 — Multiple specs observing the same event.**
  PVerifier supports it (`_specListenMap[ev]` is a list). Phase
  4's `send` macro must emit *all* dispatch calls. ChainReplication
  has only one spec, so this isn't tested by the benchmark; add a
  unit test that fires two specs from one `send`.

- **R26 — Spec state in `Inhabited GlobalState`.**
  `Specs : Type` defaults to `Unit`; the existing `default :
  GlobalState` instance must extend cleanly. Confirm M1/M2 stay
  green after the `Specs` field lands.

---

## Hand-off to Phase 5 and beyond

Phase 5 (Remaining surface) needs:
- **`map[K,V]` and `set[T]`** — ChainReplication's spec uses
  `map[int,int]`; the `kv[req.k] = req.v` write and `if (k in kv)`
  test are not yet supported. PVerifier encodes maps as
  `[K]Option V`; PLean should follow.
- **`foreach (x in S) invariant <inv>; { body }`** — heavy in
  Consensus/Paxos; not used by ChainReplication.
- **`if`/`while` inside handler bodies with full `WPGen` plumbing**
  — Lean already supports these in `do`-blocks, but the `pverify`
  unfolder needs the right `WPGen.if`/`WPGen.while` simp lemmas.
- **`assume <prop>;`** — D33 stub; Phase 5 wires it as a tactic-
  level `have h : <prop> := ?_` plus an obligation that `<prop>`
  holds in context.
- **R-P3.4** Registry-aware `is` macro lands in Phase 4, but the
  full set of error-message refinements (per-pmodule scoping for
  cross-pmodule `is` references) probably needs Phase 5.

Phase 6 (Tutorial port) needs:
- All of Phase 5's features.
- Port [`Tutorial/1_ClientServer/`](../../../Tutorial/1_ClientServer/) (M5).
- Port [`Tutorial/2_TwoPhaseCommit/`](../../../Tutorial/2_TwoPhaseCommit/) (M6).

---

## References

**In-repo**:

- [`PLAN.md`](PLAN.md) — overall plan; Phase 4 sketch at
  [§"Phase 4 — Spec machines"](PLAN.md#phase-4--spec-machines-34-days)
- [`PLAN_P3.md`](PLAN_P3.md) — Phase 3 plan; D26 deferred specs to here
- [`STATUS.md`](STATUS.md) — Phase 4 hooks at "Hand-off to Phase 4"
- [`REVIEW_P3.md`](REVIEW_P3.md) — review passes whose deferred
  items become Phase 4's R-P3.x sub-tasks
- [`Verify/Obligation.lean`](../PLean/Verify/Obligation.lean) —
  template for `Verify/SpecObligation.lean`
- [`Tests/Surface/ObligationShape.lean`](../Tests/Surface/ObligationShape.lean) —
  template for `Phase4SpecObligationShape.lean`

**Tutorial benchmark**:
- [`Tutorial/Advanced/1_ChainReplicationVerification/PSrc/System.p`](../../../Tutorial/Advanced/1_ChainReplicationVerification/PSrc/System.p) —
  M4 acceptance benchmark. Uses `spec StrongConsistency`,
  `map[int,int]`, `if (k in kv)`, `pure head()`/`tail()`/`next_()`.

**PVerifier reference**:
- [`Uclid5CodeGenerator.cs:980-1088`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L980-L1088) —
  spec flattening (the C# code Phase 4 mirrors in Lean)
- [`Uclid5CodeGenerator.cs:1981-1999`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L1981-L1999) —
  `SendStmt` patch that triggers spec dispatch (D31's reference)
