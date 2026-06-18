# CLAUDE.md — PLean

Orientation for working in `Src/PLean/`. PLean is a separate Lake
project from the parent dotnet build (`Src/PCompiler/`); the
parent-repo `CLAUDE.md` (one level up) covers P / PChecker / PeasyAI.

## What PLean is

A port of the P language and its PVerifier verification backend into
Lean 4, using [Loom](https://github.com/verse-lab/loom) as the proof
backend. The user surface mirrors P's grammar — machines, states,
events, invariants, the temporal `≺` precedence operator — and adds
verification-declaration blocks (`Lemma` / `Theorem` / `Proof`).

The user-facing pipeline:
1. Author a `pmodule M { … }` (one or more files contribute fragments).
2. `#gen_module M` synthesises Lean defs from the registered metadata.
3. `#pwf M` reports well-formedness (a fast subset check).
4. `#pverify M` walks the registry, emits one Hoare-triple obligation
   per `(machine, state, event, prove-directive)` (plus one base-case
   VC per `(directive, invariant)`), consults the `@[pverifyProof]`
   attribute for user-supplied manual proofs, and discharges the rest
   via an SMT-backed tactic chain (`pverify_smt_close` → `loom_smt`).

PLean is **not** a wrapper around PChecker or PVerifier. It's a
parallel-language port whose deliverable is "P programs verify in
Lean", with the same surface and the same per-handler obligation
shape PVerifier emits.

## Build

PLean is its own Lake project. Always work from inside `Src/PLean/`:

```bash
cd Src/PLean
lake build PLean         # the library
lake build Tests         # all tests under Tests/
lake build Examples      # the PingPong demo
lake build Tests.Surface.Phase2PingPong   # one specific test
```

`lake build` from the repo root will fail. The first build downloads
`z3` and `cvc5` solvers (~100 MB) into Loom's build directory; the
lakefile arranges this so `loom_smt` resolves them. Toolchain pinned
to **Lean v4.24.0** in `lean-toolchain`.

## Architecture

```
PLean/
  Internal/
    Decls.lean       -- metadata records (PTypeDecl, PEventDecl, ...)
    Registry.lean    -- env extension; cross-file pmodule aggregation
  Semantics/
    Label.lean       -- Label, EventOrGoto, MachineState
    GlobalState.lean -- ProgramSig, GlobalState
    Monad.lean       -- PM := NonDetT (StateT (GlobalState P) DivM)
    Primitives.lean  -- send / raise / goto / announce / markReceived
    Predicates.lean  -- inflight / sent / received / precedes / stateOf
    Default.lean     -- UniqueActions / IncreasingCount / ReceivedSubsetSent
  Surface/
    Module.lean      -- pmodule M ... end M
    Types.lean       -- type N / enum N / type N = (...)
    Events.lean      -- event ev : T
    Machine.lean     -- machine M { var ...; state S { ... } }
    Stmt.lean        -- send / raise / goto / announce / var-assign macros
    Verify.lean      -- invariant / Lemma / Theorem / Proof / system <s>
                        paxiom / init-holds / function / pinstance,
                        plus the field-projection sugar and kind-guard
                        injection passes
    Notation.lean    -- ≺, is, targets notations
  Commands/
    GenModule.lean   -- #gen_module M (synthesises types, accessors,
                        handler defs, MKind, InitConditions, lemma
                        bundles, is_* / <ev>_payload_of predicates)
    PWf.lean         -- #pwf M
    PVerify.lean     -- #pverify M (consults @[pverifyProof], then SMT)
    PrintModule.lean -- #print_pmodule M
  Verify/
    Obligation.lean  -- per-handler triple synthesis + base-case VCs
    Tactic.lean      -- pverify_open_triple / _step_wp / _intro_pre /
                        _normalize_state / _smt_close / _grind / default_inv
    ProofRegistry.lean -- @[pverifyProof] attribute + env extension
    SimpAttrs.lean   -- @[pverifySimp] simp attribute
    SimpLemmas.lean  -- the lemmas tagged with @[pverifySimp]
    CexParse.lean    -- parse + de-mangle a solver model from the SAT diag
    CexModel.lean    -- decode model into machine table + sorted sent trace

Examples/PingPong/   -- canonical surface demo
Tests/               -- Tests.** is globbed; new files auto-pick up
  Bootstrap/         -- Phase-0 regressions
  Semantics/         -- Phase-1 regressions (M1, SMT round-trip)
  Surface/           -- Phase-2 / Phase-3 regressions (M2, M3 benchmarks)
```

## Phase plans (READ THESE BEFORE NON-TRIVIAL CHANGES)

The plan docs are the authoritative design record. STATUS.md is a
living tracker, not a design doc. When PLAN.md disagrees with a phase
plan, the phase plan wins (PLAN.md predates the implementation).

- [`docs/ROADMAP.md`](docs/ROADMAP.md) — collaborator-facing entry
  point; current phase, W1–W7 workstreams, sprint allocation.
- [`docs/PLAN.md`](docs/PLAN.md) — overall phase plan.
- [`docs/PLAN_P0.md`](docs/PLAN_P0.md) — Phase 0 (Bootstrap).
- [`docs/PLAN_P1.md`](docs/PLAN_P1.md) — Phase 1 (Semantic core).
- [`docs/PLAN_P2.md`](docs/PLAN_P2.md) — Phase 2 (Registry + surface);
  decisions D8–D17, risks R8–R14.
- [`docs/PLAN_P3.md`](docs/PLAN_P3.md) — Phase 3 (Verification
  declarations); decisions D18–D28, risks R15–R21. Names the
  Tutorial/Advanced benchmarks driving M3 acceptance.
- [`docs/PLAN_P4.md`](docs/PLAN_P4.md) — Phase 4 (Spec machines);
  D29–D35 plus the residual P3 follow-ups.
- [`docs/PLAN_CEX.md`](docs/PLAN_CEX.md) — counter-example rendering
  (v1 shipped); the v1.5 / v2 follow-ups (CVC5 finite-model-find,
  pre/post diff, exact name recovery via lean-auto's `h2lMap`).
- [`docs/REVIEW_P3.md`](docs/REVIEW_P3.md) — code review against
  PLAN_P3.
- [`docs/STATUS.md`](docs/STATUS.md) — phase status, decision log,
  milestones.

## Verification benchmarks

Phase 3+ targets the verified benchmarks under
[`Tutorial/Advanced/`](../../Tutorial/Advanced/) (parent repo). PLAN_P3
has the per-benchmark feature inventory.

- **M3 (Phase 3)**: `6_DistributedLock`, `8_LockServer`,
  `3_RingLeaderVerification` — basic Theorem/Proof/Lemma blocks,
  machine-kind `is`, multi-Lemma `using` chains.
- **Phase 4 (specs)**: `1_ChainReplicationVerification`.
- **Phase 5 (foreach/maps)**: `5_Consensus`,
  `2_TwoPhaseCommitVerification`, `7_ShardedKV`.
- **Phase 6 stretch**: `4_Paxos`.

Most Tutorial/Advanced benchmarks need surface features that aren't
built yet — check PLAN_P3's "Tutorial benchmark inventory" table
before attempting one.

## Phase status (as of 2026-06-17)

- Phase 0 (Bootstrap) — ☑ M0.
- Phase 1 (Semantic core) — ☑ M1. Hand-written ping-pong verifies via
  `wpgen` + manual proof tail.
- Phase 2 (Registry + minimal surface) — ☑ M2. `#gen_module`
  synthesises per-pmodule `Sig`/`PM'`/`GS`; surface macros target the
  real PM; M2 surface ping-pong verifies.
- Phase 3 (Verification declarations) — ◐ in progress. The
  SMT-discharge pipeline, the `@[pverifyProof]` registry, and the
  `pverify_*` tactic library are in tree. Closure rates:
  `Phase3DistributedLock` 10/12, `Phase3LockServer` 17/22 — residuals
  are genuine inductiveness gaps in the ports, not infrastructure
  bugs. `3_RingLeaderVerification` not yet ported.
- Phase 4 (Spec machines) — ☐ next.

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for what's left.

## Surface keywords

PLean tracks P verbatim except where a P keyword collides with a Lean
reserved word or builtin. Mechanical replacements when porting `.p`:

| P keyword       | PLean surface  |
|-----------------|----------------|
| `module`        | `pmodule`      |
| `axiom`         | `paxiom`       |
| `instance`      | `pinstance`    |
| `pure`          | `function`     |
| `init`          | `init-holds`   |
| `do` (in `on`)  | (dropped)      |

`Lemma`, `Theorem`, `Proof`, `prove`, `using`, `system` are
new-in-PLean keywords for the verification-declaration surface
(Phase 3); `default` is a reserved sentinel inside `Proof` blocks.

## Conventions worth knowing

The plan docs and source comments cover the *why* in detail. The
points below are the load-bearing invariants you'll need to respect
when editing.

### Invariants are state-parameterised — wrap in `system <s> { … }`

Invariant bodies that mention global state must be inside a
`system <s> { … }` block. The materialiser binds `s` as the
predicate's lambda argument and the body resolves bare `s` references
to it. Outside `system`, an `invariant <name> : <body>` becomes
`fun _ => <body>` (state-independent only).

```lean
Theorem safety {
  system s {
    invariant unique_holder :
      ∀ n1 n2 : Node,
        n1.held = true → n2.held = true → n1 = n2
  }
}
```

The 2026-06-10 soundness fix (see STATUS.md decision log) made this
non-optional; a `Tests/Surface/SoundnessRegression.lean` test pins
the shape `def name : GS → Prop`, so don't reintroduce the
closed-`Prop` form. If the materialiser needs to change, update the
regression test in lockstep.

### Field-projection sugar inside `system` blocks

`n.<v>` (where `n : <M>` and `<v>` is a registered machine `var`)
desugars to `(s.machines n.ref).fields.<M>_<v>`. `e.<f>` (where
`e : <ev>` and `<f>` is a payload field) desugars to
`(<ev>_payload_of e).<f>`. The rewrite is gated on the field name
being registered, so `n.ref`, `e.action`, `e.target`, `s.machines`
pass through unchanged. Bare top-level invariants don't get the
rewrite.

The pass runs **before** kind-guard injection, in
`Surface/Verify.lean::rewriteFieldProjections`, so it can see the
original quantifier types — kind-guard injection retypes event
binders to `Sig.Label` and would defeat the lookup.

### Kind guards auto-injected on machine/event quantifiers

Inside a `system <s> { … }` block, the materialiser walks the
invariant body and adds the runtime kind check to every quantifier
over a registered machine / event kind:

| User wrote          | Materialiser emits                            |
|---------------------|-----------------------------------------------|
| `∀ n : <M>, body`   | `∀ n : <M>, is_<M> n.ref s → body`            |
| `∃ n : <M>, body`   | `∃ n : <M>, is_<M> n.ref s ∧ body`            |
| `∀ e : <ev>, body`  | `∀ e : Sig.Label, is_<ev> e → body`           |
| `∃ e : <ev>, body`  | `∃ e : Sig.Label, is_<ev> e ∧ body`           |

Same transform applies inside `init-holds`. Multi-binder forms get
normalised to nested singles before injection.

### `MachineRef` stays flat; per-machine static type is a wrapper

`MachineRef := Nat`. Per-machine static distinction lives in the
wrapper struct emitted by `#gen_module`: `structure <M> where ref :
MachineRef` plus `instance : Coe <M> MachineRef`. The runtime carrier
(state map, label target field) is keyed on the flat `MachineRef`.
The per-kind *dynamic* check goes through a `Nat` `kind` tag on
`MachineState`: `0` reserved for "unset", real kinds `≥ 1`,
`<M>_allocated` checks `kind ≠ 0 ∧ kind = <M>_kind ∧ currentState ∈
<M>'s states`. `is_<M>` is the public alias.

The `currentState ∈ <M>'s states` conjunct is load-bearing: `kind :
Nat` and `currentState : S` (a flat union of *every* machine's states)
are independent `MachineState` fields, so without it a spurious model
can fabricate a machine with one kind's tag and another kind's control
state — PVerifier's typed per-machine state arrays exclude that
structurally. It only ever weakens a guard antecedent (`is_<M> m s →
…`), so it can't make a real obligation harder, and `goto` preserves it
(a machine only transitions within its own states). For the coupling to
reach SMT, the `<S>_st` state aliases are `@[reducible]` so prep's
`dsimp only` reduces `currentState = <S>_st` to the raw `S.<M>_<S>`
constructor. [`Tests/Surface/Phase3R20.lean`](Tests/Surface/Phase3R20.lean)
pins the desync exclusion.

Don't introduce a `MachineRef <M>`-parameterised refinement; that
would diverge from PVerifier's flat encoding.

### Per-handler triples are pure consecution; init is a separate VC

A per-handler obligation is `(Inv ∧ DispatcherContract) ⇒
wp(handler, Inv)`; `InitConditions` does **not** appear there.
`Verify/Obligation.lean::emitBaseCaseObligation` discharges the
initiation leg separately, one VC per individual invariant in each
`prove G` directive's bundle. Premises (`using P`) don't get base
VCs — only goals do, matching PVerifier.

### `#pverify` is an SMT-discharge command, not a tactic engine

For each obligation it (a) consults `@[pverifyProof]` for a
user-supplied theorem matching the obligation's name, (b) emits
`theorem ... := by first | <chain> | sorry` otherwise,
(c) inspects the elaborated value with `info.value.hasSorry` to
detect failure (catches sync and async-snapshot tactic errors).
The atomic `pverify_*` tactics in `Verify/Tactic.lean` are
user-facing primitives for the manual-proof escape hatch — see
[`Tests/Surface/PVerifyManualProof.lean`](Tests/Surface/PVerifyManualProof.lean).
**Don't bake substantive automation into `#pverify` itself**;
that work belongs in user-callable tactics or `@[pverifyProof]`
theorems.

### Macro hygiene through `mkIdent`

`#gen_module` and `Surface/Stmt.lean` emit identifiers that must
resolve against user-namespace constants (`Sig`, `E`, `G`, `this`,
`<S>_st`, `<v>_get`, …). Bare names inside `` `(...) `` quotations
get hygiene marks during expansion and fail to resolve. Convention:
any identifier that needs to resolve against a user-namespace constant
is built via `mkIdent` and spliced. **If you see `PLean.DivM✝` or
`Sig✝` in an error message, hygiene is the cause.**

### `loom_solve` is CaseStudies-only and doesn't fit PLean

PLean's lakefile only requires the `Loom` lib, not `CaseStudies`.
`loom_solve` queries assertion data registered by Cashmere's `bdef`
macro, which PLean does not produce. `Verify/Tactic.lean` recomposes
the underlying pieces (`wpgen` + simp set + Veil-style preprocessing
for `GlobalState` + `loom_smt`) without that scaffolding.

### SMT preparation: tag new state-update functions with `@[pverifySimp]`

`GlobalState`-shaped goals reach `loom_smt` through a preprocessing
chain (`pverify_smt_prep`) that turns function-typed record fields
into applied uninterpreted symbols lean-auto can translate. The
recipe lives in `Verify/Tactic.lean`; the simp set it relies on is
`@[pverifySimp]` (declared in `Verify/SimpAttrs.lean`, populated in
`Verify/SimpLemmas.lean`). When you add a new `GlobalState` update
helper or predicate that should reduce before SMT, tag it with
`@[pverifySimp]`.
[`Tests/Semantics/SmtVeilRecipe.lean`](Tests/Semantics/SmtVeilRecipe.lean)
pins the recipe on the three default invariants.

### Counter-examples are decoded, not dumped

A disproved obligation routes its solver model through
`Verify/CexParse.lean` + `Verify/CexModel.lean` (called from
`Obligation.lean::renderCex`) into a per-machine state table
(`Node@Act(epoch=9, held=false)`), the `sent` trace ordered by
`actionCount` (`eGrant(node=Node#8, epoch=7)`, `[]` when empty), and a
witnesses section. Machine refs render as `<Kind>#<ref>` labels; since
`MachineRef` is a reducible `Nat`, ref-typed fields are found from
projection return types and kinds from the `machines` table (a ref not
in `machines` renders bare, `#24`). Two name sources combine: string
de-mangling of lean-auto's `"_" ++ delab(expr)` atoms (exact `h2lMap`
recovery is the v2 follow-up), and a `CexNameCtx` built from the registry
in `Obligation.lean::buildCexNameCtx` and passed via `cexNameCtxRef`
(state-ctor → machine/state, global `Fields` order, event payload field
names, ref-typed field names). All names are read from the materialised
structures via the **environment** (`getStructureInfo?` + projection
types), NOT the registry `defStx` — `#gen_module` clears `defStx` before
`synthesise` runs. Anything without a name degrades to a de-mangled raw
value. Don't pin exact model *values* in tests — they're solver-specific;
pin de-mangling and structural markers (see
[`Tests/Verify/CexEndToEnd.lean`](Tests/Verify/CexEndToEnd.lean)). The
synthetic-model goldens in
[`Tests/Verify/CexParserGolden.lean`](Tests/Verify/CexParserGolden.lean)
pin exact rendering with a hand-built `CexNameCtx`.

## Common operations

### Add a new test

Drop the file under `Tests/Bootstrap/`, `Tests/Semantics/`, or
`Tests/Surface/` — the `Tests` lean_lib globs `Tests.**`.

### Iterate on a failing test

```bash
cd Src/PLean
lake build Tests.Surface.MyTest 2>&1 | grep -E '^error:' -A 5
```

For a quick syntax-only check while iterating, the IDE diagnostics in
the Lean extension catch most issues without re-running `lake`.

### Run cvc5 / z3 from Lean

The solvers are downloaded into Loom's build dir on first build.
To verify they're reachable:

```bash
lake build Tests.Semantics.SmtRoundtrip
# expect: "Goal proven by cvc5. Trusting SMT solver result."
```
