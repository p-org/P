# CLAUDE.md — PLean

This file provides guidance for working in `Src/PLean/`. The
parent-project `CLAUDE.md` (one level up at `Src/../CLAUDE.md`) covers
P / PChecker / PeasyAI; PLean is a separate sub-project with its own
build system and conventions documented here.

## What PLean is

A port of the P language and its PVerifier verification backend into
Lean 4, using [Loom](https://github.com/verse-lab/loom) (the verse-lab
verification framework) as the proof backend. The user surface mirrors
P's grammar (machines, states, events, invariants, ≺ for temporal
precedence); `#gen_module M` synthesises Lean defs from the registry;
`#pverify M` walks the registry, synthesises one Hoare-triple
obligation per `(machine, state, event)`, consults the
`@[pverifyProof]` attribute for user-supplied manual proofs, and
discharges the rest via an SMT-backed tactic chain that PLean owns.

PLean is **not** a wrapper around PChecker or PVerifier. It's a
parallel-language port whose deliverable is "P programs verify in
Lean", with the same surface and the same per-handler obligation
shape PVerifier emits.

## Build

PLean is its own Lake project, **not** part of the dotnet build at
`Src/PCompiler/`. Always work from inside `Src/PLean/`:

```bash
cd Src/PLean
lake build PLean         # the library
lake build Tests         # all tests under Tests/
lake build Examples      # the PingPong demo
lake build Tests.Surface.Phase2PingPong   # one specific test
```

`lake build` from the repo root will fail with `no configuration file
… /Users/.../P/lakefile.lean`. Always `cd Src/PLean` first.

The first build downloads `z3` and `cvc5` solvers (~100MB) into
`.lake/packages/Loom/.lake/build/`. This is intentional — Loom's
`loom_smt` resolves binaries relative to its own source dir, and the
lakefile arranges the download to land at the right place. See
`lakefile.lean` for the wiring.

The toolchain is pinned to Lean **v4.24.0** in `lean-toolchain`.

## Architecture

```
PLean/
  Internal/
    Decls.lean              -- metadata records (PTypeDecl, PEventDecl, ...)
    Registry.lean           -- env extension; cross-file pmodule aggregation
  Semantics/
    Label.lean              -- Label, EventOrGoto, MachineState
    GlobalState.lean        -- ProgramSig, GlobalState
    Monad.lean              -- PM := NonDetT (StateT (GlobalState P) DivM)
    Primitives.lean         -- send/raise/goto/announce/markReceived
    Predicates.lean         -- inflight/sent/received/precedes/Label.targets?/stateOf
    Default.lean            -- UniqueActions/IncreasingCount/ReceivedSubsetSent
  Surface/
    Module.lean             -- pmodule M ... end M
    Types.lean              -- type N / enum N / type N = (...)
    Events.lean             -- event ev : T
    Machine.lean            -- machine M { var ...; state S { ... } }
    Stmt.lean               -- send/raise/goto/announce/var-assign macros
    Verify.lean             -- invariant / Lemma / Theorem / Proof / system <s> { ... }
                               paxiom / init-holds / function / pinstance
    Notation.lean           -- ≺, is, targets notations
  Commands/
    GenModule.lean          -- #gen_module M (synthesises Sig, emits handlers,
                               machine wrappers, accessors, MKind, InitConditions,
                               lemma bundles, is_* predicates)
    PWf.lean                -- #pwf M (well-formedness)
    PVerify.lean            -- #pverify M (SMT-discharge command;
                               consults @[pverifyProof] before SMT)
    PrintModule.lean        -- #print_pmodule M (debug)
  Verify/
    Obligation.lean         -- per-handler triple synthesis from registry
    Tactic.lean             -- pverify_open_triple / _step_wp / _intro_pre /
                               _normalize_state / _smt_close / _grind / default_inv
    ProofRegistry.lean      -- @[pverifyProof] attribute + persistent env extension
    SimpAttrs.lean          -- @[pverifySimp] simp attribute (state-update unfolds)

Examples/
  PingPong/                 -- the canonical surface demo

Tests/
  Bootstrap/                -- Phase-0 regressions: registration, errors, multi-file
  Semantics/                -- Phase-1 regressions:
                               StackSpike (instance synthesis)
                               HandPingPong (M1 — hand-written triples)
                               Combinators (.run-based primitive tests)
                               SmtRoundtrip (cvc5 wiring)
                               SmtVeilRecipe (Veil-style preprocessing pin)
  Surface/                  -- Phase-2 / Phase-3 regressions:
                               Phase2PingPong (M2 — surface triples + #pverify)
                               Phase2PingPong_manual (M2 hand-written tail)
                               Combinators (surface .run-based tests)
                               Phase3PingPong (trivial-handler #pverify auto-discharge)
                               Phase3DistributedLock / Phase3LockServer (M3, partial)
                               Phase3Parse / Phase3Errors / Phase3DuplicateTarget /
                                 Phase3R20 (registration + error-path pins)
                               PVerifyTactic / PVerifyManualProof /
                                 PVerifyProofRegistry / PVerifyConditional
                                 (tactic-library + @[pverifyProof] workflow)
                               ObligationShape (#guard_msgs-pinned theorem shape)
                               SoundnessRegression (system <s> binder soundness pin)
```

## Phase-by-phase planning docs (READ THESE FIRST)

Every substantive change should consult the relevant plan doc. They
are **the authoritative design record** — STATUS.md is a living
tracker, not a design doc.

- [`docs/ROADMAP.md`](docs/ROADMAP.md) — collaborator-facing entry
  point; current phase status, outstanding work as W1–W7
  workstreams, division-of-effort tables. Start here for sprint
  planning or onboarding.
- [`docs/PLAN.md`](docs/PLAN.md) — overall plan; phase checkboxes
- [`docs/PLAN_P0.md`](docs/PLAN_P0.md) — Phase 0 (Bootstrap)
- [`docs/PLAN_P1.md`](docs/PLAN_P1.md) — Phase 1 (Semantic core)
- [`docs/PLAN_P2.md`](docs/PLAN_P2.md) — Phase 2 (Registry + surface);
  decisions D8–D17, risks R8–R14
- [`docs/PLAN_P3.md`](docs/PLAN_P3.md) — Phase 3 (Verification
  declarations); decisions D18–D28, risks R15–R21. Names the
  Tutorial/Advanced benchmarks that drive M3 acceptance.
- [`docs/PLAN_P4.md`](docs/PLAN_P4.md) — Phase 4 (Spec machines);
  decisions D29–D35, plus the residual P3 follow-ups (R-P3.1..7)
  collected so a P3-then-P4 reader sees the full debt
- [`docs/REVIEW_P3.md`](docs/REVIEW_P3.md) — three-pass code review
  against PLAN_P3 / STATUS — drives the deferred-items list
- [`docs/STATUS.md`](docs/STATUS.md) — phase status, decision log,
  milestones, anticipated risks

When PLAN.md and a phase plan disagree, the phase plan wins. PLAN.md
predates the implementation; PLAN_P{0..4} were written against the
*pinned* Loom revision and reflect what actually shipped.

## Verification benchmarks (Tutorial/Advanced)

Phase 3+ targets the verified benchmarks under
[`Tutorial/Advanced/`](../../Tutorial/Advanced/) (parent repo, not
under `Src/`). PLAN_P3 has the per-benchmark feature inventory.

- **Phase 3 (M3) acceptance set**: `6_DistributedLock`,
  `8_LockServer`, `3_RingLeaderVerification` — exercise basic
  Theorem/Proof/Lemma blocks, machine-kind `is`, and
  multi-Lemma `using` chains respectively.
- **Phase 4 (specs)**: `1_ChainReplicationVerification` (has a
  `spec StrongConsistency observes ...`).
- **Phase 5 (foreach/maps)**: `5_Consensus`, `2_TwoPhaseCommitVerification`
  variants, `7_ShardedKV`.
- **Phase 6 stretch**: `4_Paxos` — full Paxos protocol, ~80 invariants.

Don't try to handle anything from `Tutorial/Advanced/` in Phase 0–2
work without checking PLAN_P3's "Tutorial benchmark inventory" table
first — most need surface features that aren't built yet.

## Phase status (as of 2026-06-11)

- Phase 0 (Bootstrap) — ☑ M0 reached.
- Phase 1 (Semantic core) — ☑ M1 reached. Hand-written ping-pong
  verifies via `wpgen` + manual proof tail.
- Phase 2 (Registry + minimal surface) — ☑ M2 reached.
  `#gen_module` synthesises per-pmodule `Sig`/`PM'`/`GS`; surface
  macros target the real PM; M2 surface ping-pong verifies in
  `Tests/Surface/Phase2PingPong.lean`. `Internal/Stub.lean` is
  deleted.
- Phase 3 (Verification declarations) — ◐ in progress. The
  SMT-discharge `#pverify` pipeline, the `@[pverifyProof]` registry,
  and the `pverify_*` tactic library are in tree and load-bearing.
  M3: `Phase3DistributedLock` (9 proved / 3 disproved) and
  `Phase3LockServer` (12 proved / 9 disproved / 1 unknown) — the
  residual obligations are genuine inductiveness gaps in the ports, not
  infrastructure bugs. Closing them is a matter of porting the missing
  invariants from the original P sources or supplying `@[pverifyProof]`
  manual proofs. The `3_RingLeaderVerification` benchmark is now ported
  in `Tests/Surface/Phase3RingLeader.lean` and **fully verifies, 32/32**
  (30 by SMT + 2 by `@[pverifyProof]` manual proofs, with
  `pverify.failOnIncomplete` at its default `true`). Porting it produced
  three reusable framework fixes:

  1. **`goto` hygiene** — `<Mod>.G`'s `unit` constructor was emitted
     hygienically, so the `goto` doElem macro (first exercised inside an
     `on`-handler by this benchmark) couldn't resolve `G.unit`. Now
     emitted unhygienically in `emitProgramUnions`.
  2. **Machine-state defunctionalisation** — lean-auto rejected any
     goal reading `(s.machines m).currentState` ("Higher order input?")
     because `machines : MachineRef → MachineState` is an
     array-of-records. `pverify_smt_prep` now runs
     `pverify_defunctionalize_machines`, abstracting each scalar/enum
     machine-state projection into a fresh uninterpreted
     `MachineRef → _` function before `loom_smt`. This removed the
     higher-order failures across all benchmarks (LockServer jumped
     2→12 proved) and is the general fix for the SMT higher-order
     limitation.
  3. **`InStart` init modelling + reducible state aliases** —
     `emitInitConditions` now asserts every machine begins in a start
     state, and `<S>_st` aliases are `abbrev` (reducible) so the solver
     sees the raw `S` constructors. Together these close the two
     state-dependent base cases (`LeaderMax` / `UniqueLeader`).

  The two RingLeader inductive steps through the `goto Won` handler
  (`lemmas` and `Safety`) are discharged by `@[pverifyProof]` manual
  proofs in the test file. `Safety` needed only a one-fact
  instantiation hint (`SelfPendingMax` ⇒ `this` is the global max);
  `lemmas` needed the full cyclic-betweenness argument (`btw_1`..`btw_4`)
  to show forwarding `eNominate` to the ring successor preserves
  `NoBypass`. Both are self-contained in the test file — no library
  change — and serve as the worked template for the `@[pverifyProof]`
  escape hatch on jointly-inductive ring invariants.
- Phase 4 (Spec machines) — ☐ next. Plan in PLAN_P4.

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the workstream-level
breakdown of what's left and the suggested sprint allocation.

### Soundness pin

The 2026-06-10 fix made invariants state-parameterised via the
`system <s> { … }` block. Pre-fix, the materialiser emitted closed
`Prop` invariants and the bundle predicate `fun _ => name ∧ True`
discarded its state argument, letting `#pverify` "verify" trivially
false safety properties. The current materialiser emits
`def name : GS → Prop := fun s => <body>` (or `fun _ => <body>` for
state-independent invariants) and rejects an inner-`∀ s : GlobalState
Sig, …` shadowing pattern. Regression pinned in
[`Tests/Surface/SoundnessRegression.lean`](Tests/Surface/SoundnessRegression.lean).
**Don't reintroduce the closed-`Prop` shape**; if the materialiser
needs to change, update the regression test in lock-step.

## Conventions worth knowing

### Surface keywords vs. P keywords

PLean's surface tracks P verbatim except where a P keyword collides
with a Lean reserved word or builtin. The collisions are:

| P keyword       | PLean surface  | Reason                                                       |
|-----------------|----------------|--------------------------------------------------------------|
| `module`        | `pmodule`      | Lean reserves `module`                                       |
| `axiom`         | `paxiom`       | Lean's builtin `axiom` command intercepts before our gating  |
| `instance`      | `pinstance`    | Lean's builtin `instance` command intercepts before gating   |
| `pure`          | `function`     | Lean's `pure ()` term parses first; renamed for natural reading |
| `init`          | `init-holds`   | Avoids Lean's `(init := …)` named-argument syntax            |
| `do` (in `on`)  | (dropped)      | Lean's tokenizer eagerly consumes `do` as a do-block opener  |

Tutorial/Advanced ports therefore differ from their `.p` source by a
small set of mechanical replacements:

```
P:      pure lock_server() : machine        →   PLean: function lock_server : MachineRef
P:      init <prop>                         →   PLean: init-holds <prop>
P:      axiom <name> : <prop>               →   PLean: paxiom <name> : <prop>
P:      module M { … }                      →   PLean: pmodule M { … }
P:      on ev do (p : T) { … }              →   PLean: on ev (p : T) { … }
```

Capitals (`Lemma`, `Theorem`, `Proof`, `prove`, `using`) are
new-in-PLean keywords introduced for the verification-declaration
surface (Phase 3); `default` is a reserved sentinel used inside
`Proof` blocks but not a reserved Lean token.

### Macro hygiene through `mkIdent`

`#gen_module` and `Surface/Stmt.lean` emit identifiers that must
resolve against user-namespace constants (`Sig`, `E`, `G`, `this`,
`<S>_st`, `<v>_get`, ...). Bare names inside macro quotations
`` `(...) `` get hygiene marks during expansion and fail to resolve
against those constants. The convention: any identifier that needs
to resolve against a user-namespace constant is constructed via
`mkIdent` and spliced in. File headers in `Surface/Stmt.lean` and
`Commands/GenModule.lean` document this in detail. **If you see
`PLean.DivM✝` or `Sig✝` in an error message, hygiene is the cause.**

### Inhabited / DecidableEq derives

`<Mod>.E` requires `DecidableEq` derive recursively over event
payload types. Phase 2 added `deriving Inhabited, DecidableEq` to
named-tuple struct emission in `Surface/Types.lean`. If a user
declares an event with a payload that doesn't auto-derive
`DecidableEq`, the `<Mod>.E` derive fails — fix is at the payload
type, not at `<Mod>.E`.

`<Mod>.E`'s `Inhabited` instance uses an explicit
`⟨E.<firstEv> default⟩` rather than `deriving Inhabited` because
Lean's default-derive picks the first ctor and requires it Inhabited
on its own. Check `emitProgramUnions` in `Commands/GenModule.lean`.

### Open-of `PartialCorrectness DemonicChoice` is per-call, not file-scope

`Loom`'s `MAlgOrdered` instances for `NonDetT` and `DivM` are
`scoped` inside `PartialCorrectness DemonicChoice`. The
`#derive_lifted_wp` calls in `Commands/GenModule.lean` wrap each
emission in `open PartialCorrectness DemonicChoice in ...` per-call —
**don't** bake the open into a file-level `import` chain. Doing that
would force every importer of a generated pmodule into the
partial-correctness + demonic-choice mode globally, which we want as
a per-test-file decision (M1, M2 both `open` it explicitly at the
test file).

### `loom_solve` is CaseStudies-only and doesn't fit PLean

PLean's lakefile only requires the `Loom` lean_lib, not
`CaseStudies`. Even when `CaseStudies.Tactic` is imported, its
`loom_solve` tactic queries `loomAssertionsMap` for `WithName`-
registered assertions — registration that Cashmere's `bdef` macro
does, which PLean does not. Phase-2 confirmed this empirically
(`"Failed to parse an assertion without names: WPGen (liftM get)"`).
PLean's `Verify/Tactic.lean` recomposes the underlying pieces
(`wpgen` + Loom's logic-simp set + Veil-style preprocessing for
`GlobalState` field-functions + `loom_smt`) without the `WithName`
scaffolding. The user-facing primitives are `pverify_open_triple`,
`pverify_step_wp`, `pverify_intro_pre`, `pverify_normalize_state`,
`pverify_smt_close`, `pverify_grind`, plus the head-symbol-gated
`default_inv` for the three default-invariant goals.

### `#pverify` is an SMT-discharge command, not a tactic engine

The 2026-06-09 architectural pivot landed `#pverify` as a registry
walker that, per obligation, (a) consults the `@[pverifyProof]`
attribute for a user-supplied theorem matching the obligation's
shape, (b) emits `theorem ... := by first | <pverify chain> | sorry`
otherwise, (c) inspects the elaborated value with
`info.value.hasSorry` to detect failure (catches both sync and
async-snapshot tactic errors). The output format is
`<modName>: N obligations from K prove-directives (M proved by SMT,
J user-proved, L failed)`. The atomic `pverify_*` tactics in
`Verify/Tactic.lean` are user-facing primitives for the manual-proof
escape hatch — see [`Tests/Surface/PVerifyManualProof.lean`](Tests/Surface/PVerifyManualProof.lean)
for the workflow. **Don't bake substantive automation into
`#pverify` itself**; that work belongs in the user-callable tactics
or in `@[pverifyProof]`-tagged theorems.

### Asymmetric pre/post in handler triples is structural, not a hack

A per-handler obligation is **not** "from Inv, prove Inv". The
precondition adds the *dispatcher contract* — the framework's runtime
guarantee that this handler is only fired when an inflight label of
the right shape exists targeting this machine. M1 carries this via a
`lbl : Lbl` parameter and explicit `inflight lbl s ∧ lbl.action = ...`
clauses; M2's surface signature drops `lbl`, so we existentially
quantify the witness in the precondition (`∃ p, sent p ∧ p is ePing
∧ p.actionCount < s.actionCount`). Without it, `Inv` is too weak:
the empty state satisfies `Inv` vacuously, and a buggy dispatcher
firing a handler from there would let invariants break. See PLAN_P3
D18 for the synthesised form.

### `is` notation: ctor-tag check, currently not registry-aware

`Surface/Notation.lean` has `lbl is <ev>` as a *macro* (not a plain
notation) that rewrites to `is_<ev> lbl`. The same form covers
machine-kind RHS (`m is Server` rewrites to `is_Server m`); Lean's
name resolution picks whichever `is_<rhs>` exists. **The macro is
not registry-aware today** — a typo on the RHS surfaces as a
generic "unknown identifier" error rather than a bespoke "unknown
event or machine" message. Tracked as a Phase-3 follow-up. Don't
treat `is` as if it were `Eq` — for events it's a ctor-tag check
(no payload equality) per P semantics.

### `MachineRef` stays flat

`MachineRef := Nat`. Per-machine *static* type distinction lives in
the wrapper structs: `structure Server where ref : MachineRef` plus
`instance : Coe Server MachineRef`. The runtime carrier (state map,
label target field) is keyed on `MachineRef`. The per-kind *dynamic*
check (`m is Server` for an underlying `MachineRef`) goes through a
flat `Nat` kind tag on `MachineState`: `0` is reserved for "unset",
real kinds are `≥ 1`, and `<M>_allocated` checks
`kind ≠ 0 ∧ kind = <M>_kind`. The per-pmodule `<Mod>.MKind` inductive
exists for documentation but the runtime field is `Nat`. Don't
introduce a `MachineRef Server`-parameterised refinement; it would
diverge from PVerifier's flat encoding.

### Invariants are state-parameterised — use `system <s> { … }`

User invariants whose body refers to global state must be wrapped in
a `system <s> { … }` block; the materialiser binds `s` as the
predicate's lambda argument and the body resolves bare `s`
references against it.

```lean
Theorem safety {
  system s {
    invariant unique_holder :
      ∀ n1 n2 : Node,
        Node_allocated n1.ref s → Node_allocated n2.ref s →
        (s.machines n1.ref).fields.Node_held = true →
        (s.machines n2.ref).fields.Node_held = true →
        n1 = n2
  }
}
```

Outside a `system` block, an `invariant <name> : <body>` is
materialised as `fun _ => <body>` and any reference to global state
inside the body fails to resolve. Inside a `system` block, an inner
`∀ s : GlobalState Sig, …` shadowing pattern is detected and
rejected at materialisation time. This is the soundness pin —
[`Tests/Surface/SoundnessRegression.lean`](Tests/Surface/SoundnessRegression.lean)
keeps it honest.

### Manual-proof escape hatch via `@[pverifyProof]`

When `#pverify`'s SMT chain cannot close an obligation, the failure
report prints a copy-paste skeleton:

```lean
@[pverifyProof]
theorem <Mod>.<M>.<S>.<ev>_correct_<X>
    (this : <M>) (param : <ev>_payload) :
    triple (l := PProp Sig) (fun s => …) (handler this param) (fun _ s => …) := by
  pverify_open_triple
  pverify_step_wp
  pverify_intro_pre ⟨…⟩
  pverify_normalize_state
  pverify_smt_close   -- or pverify_grind, or hand-finish
```

Pasting the skeleton into the source file with a real proof body
makes `#pverify` pick it up on the next run via the `pverifyProofExt`
env extension keyed on theorem name. The
`pverify.failOnIncomplete` option (default `true`) makes
`#pverify` throw on residual failures; setting it to `false` lets a
file build with `sorry`-padded skeletons while the user iterates.

[`Tests/Surface/PVerifyManualProof.lean`](Tests/Surface/PVerifyManualProof.lean)
shows the workflow end-to-end;
[`Tests/Surface/PVerifyProofRegistry.lean`](Tests/Surface/PVerifyProofRegistry.lean)
exercises the auto vs. manual paths side-by-side.

### SMT preparation: Veil-style recipe before `loom_smt`

`pverify_smt_close` cannot pass `GlobalState`-shaped goals directly
to lean-auto / `loom_smt` — the function-typed record fields
(`sent : Label → Bool`, `machines : MachineRef → MachineState`)
trigger a "higher-order input" rejection. The fix is preprocessing,
not a different state shape: `intros → simp [pverifySimp] →
sdestruct_state → unfold WithName → dsimp only → unfold
DefaultInvariants / UniqueActions / IncreasingCount /
ReceivedSubsetSent at *`. After this, function-typed fields appear
only in *applied* form (`s.sent lbl`, never `s.sent` standalone) and
lean-auto translates them as uninterpreted function symbols. The
recipe lives inside `pverify_smt_prep` in `Verify/Tactic.lean`;
`pverify_smt_close` runs it before invoking `loom_smt [*]`.
Tagging new `GlobalState` update functions with `@[pverifySimp]`
keeps them in the recipe's reach.

[`Tests/Semantics/SmtVeilRecipe.lean`](Tests/Semantics/SmtVeilRecipe.lean)
pins this on the three default invariants.

## Common operations

### Add a new test

```bash
# Surface tests live under Tests/Surface/
# (or Tests/Bootstrap/ for Phase 0-style registration tests,
#  or Tests/Semantics/ for Phase 1-style hand-written semantics tests)
```

The `Tests` lean_lib globs `Tests.**`, so any new file gets picked up.

### Iterate on a failing test

```bash
cd Src/PLean
lake build Tests.Surface.MyTest 2>&1 | grep -E '^error:' -A 5
```

For a quick syntax-only check while iterating, the IDE diagnostics
in the Lean extension catch most issues without re-running `lake`.

### Audit for `Stub` references (should be zero)

```bash
grep -rn 'PLean\.Stub\|PLean\.Internal\.Stub' \
  PLean Examples Tests 2>/dev/null
```

`Internal/Stub.lean` was deleted once `Surface/Stmt.lean` macros
repointed onto the real PM at end of Phase 2; any remaining hit is
a regression.

### Run cvc5 / z3 from Lean

The solvers are downloaded into Loom's build dir on first build. To
verify they're reachable:

```bash
lake build Tests.Semantics.SmtRoundtrip
# expect: "Goal proven by cvc5. Trusting SMT solver result."
```
