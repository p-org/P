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
   via an SMT-backed tactic chain (`pverify_smt` → `loom_smt`).

PLean is **not** a wrapper around PChecker or PVerifier. It's a
parallel-language port whose deliverable is "P programs verify in
Lean", with the same surface and the same per-handler obligation
shape PVerifier emits.

## Build

PLean is its own Lake project. Always work from inside `Src/PLean/`:

```bash
cd Src/PLean
lake build PLean                     # the library
lake build Tests                     # all regressions under Tests/
lake build Examples                  # protocol ports (PingPong, DistributedLock, …)
lake build Examples.DistributedLock  # one specific protocol
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
  Syntax/
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
    Tactic.lean      -- pverify / pverify_step_wp / pverify_smt /
                        pverify_split_smt / default_inv / clause helpers
    ProofRegistry.lean -- @[pverifyProof] attribute + env extension
    SimpAttrs.lean   -- @[pverifySimp] simp attribute
    SimpLemmas.lean  -- the lemmas tagged with @[pverifySimp]
    CexParse.lean    -- parse + de-mangle a solver model from the SAT diag
    CexModel.lean    -- decode model into machine table + sorted sent trace

Examples/            -- protocol ports (showcased benchmarks)
  PingPong/          --   multi-file PingPong demo
  PingPongAuto.lean  --   trivial PingPong with #pverify auto-discharge
  PingPongManual.lean--   PingPong with hand-written triples
  PingPongTrivial.lean--  smoke test for the trivial-handler auto path
  DistributedLock.lean--  6_DistributedLock port (12/12)
  LockServer.lean    --   8_LockServer port (37/37)
  RingLeader.lean    --   3_RingLeaderVerification port (17/17)
  ClockBound.lean    --   AWS-style clock-bound daemon (59/59) — exercises
                     --   `PLean.choose` (bounded nondet Int)

Tests/               -- regressions; Tests.** is globbed
  Bootstrap/         --   Phase-0 #pwf / multi-file aggregation
  Semantics/         --   PM stack + SMT round-trip
  Syntax/            --   surface-syntax + #pverify regressions
                     --   (Errors, Parse, MachineKindIs, ObligationShape,
                     --    FieldProjectionSugar, PVerify*, PAxiomProbe,
                     --    PInstanceExercise, HandlerCoverage,
                     --    SoundnessRegression, …)
  Verify/            --   pverify infrastructure (cache, CEX, profile,
                     --   ManualProofHelpers)
```

## Phase plans (READ THESE BEFORE NON-TRIVIAL CHANGES)

The plan docs are the authoritative design record. When PLAN.md
disagrees with a phase plan, the phase plan wins (PLAN.md predates
the implementation).

- [`docs/ROADMAP.md`](docs/ROADMAP.md) — collaborator-facing entry
  point; current phase, workstreams, sprint allocation.
- [`docs/PLAN.md`](docs/PLAN.md) — overall phase plan.
- [`docs/PLAN_P0.md`](docs/PLAN_P0.md) — Phase 0 (Bootstrap).
- [`docs/PLAN_P1.md`](docs/PLAN_P1.md) — Phase 1 (Semantic core).
- [`docs/PLAN_P2.md`](docs/PLAN_P2.md) — Phase 2 (Registry + surface).
- [`docs/PLAN_P3.md`](docs/PLAN_P3.md) — Phase 3 (Verification
  declarations). Names the Tutorial/Advanced benchmarks driving M3.
- [`docs/PLAN_P4.md`](docs/PLAN_P4.md) — Phase 4 (Spec machines).
- [`docs/PLAN_CEX.md`](docs/PLAN_CEX.md) — counter-example rendering;
  v1 shipped, v1.5 / v2 follow-ups listed (CVC5 finite-model-find,
  pre/post diff, exact name recovery via lean-auto's `h2lMap`).

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

## Phase status (as of 2026-06-24)

- Phase 0 (Bootstrap) — ☑ M0.
- Phase 1 (Semantic core) — ☑ M1. Hand-written ping-pong verifies via
  `wpgen` + manual proof tail.
- Phase 2 (Registry + minimal surface) — ☑ M2. `#gen_module`
  synthesises per-pmodule `Sig`/`PM'`/`GS`; surface macros target the
  real PM; M2 surface ping-pong verifies.
- Phase 3 (Verification declarations) — ☑ M3 reached. All three
  Tutorial/Advanced benchmarks fully verify:
  - [`Examples/DistributedLock`](Examples/DistributedLock.lean) — **12/12**
    (11 SMT + 1 manual `@[pverifyProof]`),
  - [`Examples/LockServer`](Examples/LockServer.lean) — **37/37**
    (34 SMT + 3 manual),
  - [`Examples/RingLeader`](Examples/RingLeader.lean) — **17/17**
    (14 SMT + 3 manual). Entry handler obligation added by the
    handler-coverage fix; see "VC completeness" below.
  - [`Examples/ClockBound`](Examples/ClockBound.lean) — **59/59**
    (58 SMT + 1 manual). Off-tree benchmark from
    [PInfer-Benchmarks](https://github.com/AD1024/PInfer-Benchmarks/tree/main/ClockBound);
    exercises `PLean.choose` (bounded nondet `Int`) and per-target
    monotonicity safety properties from `goals.json`.

  The user-facing surface for axiomatic facts is `paxiom` (single
  proposition) and `pinstance` (Veil-style typeclass bundle). The
  obligation generator injects every pmodule axiom — both hand-written
  `paxiom`s and fields synthesised from `pinstance` — into every VC's
  local context as `have hax_<name> := @<name>`, so `loom_smt [*]`
  (which only sees the lctx) can use them. Pinned by
  [`Tests/Syntax/PAxiomProbe.lean`](Tests/Syntax/PAxiomProbe.lean)
  and [`Tests/Syntax/PInstanceExercise.lean`](Tests/Syntax/PInstanceExercise.lean).

  Reusable manual-proof tactics (`pverify_carry_after_recv`,
  `pverify_not_inflight` / `_by`, `pverify_inflight_by`,
  `pverify_machine_has_type`, plus `pverify_split_smt`) cover the
  five routing-clause + kind-bridge shapes the M3 proofs ran into;
  their docstrings in [`PLean/Verify/Tactic.lean`](PLean/Verify/Tactic.lean)
  spell out the calling pattern. An obligation cache at
  `<project>/.lake/build/pverify_cache/` hashes `(lctx, goal target)`
  and shaves 11–14% off warm rebuilds; soundness pinned by
  [`Tests/Verify/CacheSoundness.lean`](Tests/Verify/CacheSoundness.lean).
- Phase 4 (Spec machines) — ☐ next. Plan in [`docs/PLAN_P4.md`](docs/PLAN_P4.md).

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for what's left.

## Surface keywords

PLean tracks P verbatim except where a P keyword collides with a Lean
reserved word or builtin. Mechanical replacements when porting `.p`:

| P keyword       | PLean surface          |
|-----------------|------------------------|
| `module`        | `pmodule`              |
| `axiom`         | `paxiom`               |
| `instance`      | `pinstance`            |
| `pure`          | `function`             |
| `init`          | `init-holds`           |
| `do` (in `on`)  | (dropped)              |
| `choose(n)`     | `← PLean.choose n`     |

`Lemma`, `Theorem`, `Proof`, `prove`, `using`, `system` are
new-in-PLean keywords for the verification-declaration surface
(Phase 3); `default` is a reserved sentinel inside `Proof` blocks.

`PLean.choose : Int → PM Int` returns a nondeterministically-chosen
`Int` in `[0, bound]`. Implemented via `MonadNonDet.pickSuchThat`;
the WP spec (registered as `@[loomSpec]`) gives the verifier `0 ≤ x
∧ x ≤ bound` as a hypothesis at the call site. See
[`Examples/ClockBound.lean`](Examples/ClockBound.lean) for usage.

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

A `Tests/Syntax/SoundnessRegression.lean` test pins the shape
`def name : GS → Prop`, so don't reintroduce the closed-`Prop` form.
If the materialiser needs to change, update the regression test in
lockstep.

### Field-projection sugar inside `system` blocks

`n.<v>` (where `n : <M>` and `<v>` is a registered machine `var`)
desugars to `(s.machines n.ref).fields.<M>_<v>`. `e.<f>` (where
`e : <ev>` and `<f>` is a payload field) desugars to
`(<ev>_payload_of e).<f>`. The rewrite is gated on the field name
being registered, so `n.ref`, `e.action`, `e.target`, `s.machines`
pass through unchanged. Bare top-level invariants don't get the
rewrite.

The pass runs **before** kind-guard injection, in
`Syntax/Verify.lean::rewriteFieldProjections`, so it can see the
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
constructor. [`Tests/Syntax/MachineKindIs.lean`](Tests/Syntax/MachineKindIs.lean)
pins the desync exclusion.

Don't introduce a `MachineRef <M>`-parameterised refinement; that
would diverge from PVerifier's flat encoding.

### Bundle conjunctions don't carry a trailing `True`

The conjunction builders — `buildConjAt` (obligation pre/post, in
`Verify/Obligation.lean`) and `emitConjPredicate` (every lemma /
theorem / `UserInv` / `InitConditions` bundle, in
`Commands/GenModule.lean`) — emit a right-associated chain `p1 ∧ p2
∧ … ∧ pn` with no `True` terminator. Empty bundles still collapse to
`fun _ => True` (correct identity).

`emitConjPredicate` is the single emission point: it accepts an
`Array (TSyntax × Bool)` so a caller can mix state-applied (`(m) s`)
and verbatim conjuncts in one call (used by `emitInitConditions` to
combine framework clauses with closed `init-holds` props).

Why this matters when editing the codegen: a manual proof registered via
`@[pverifyProof]` is **type-checked** against the obligation the
generator would emit, and a stated triple of the form `... ∧ True` no
longer matches. The corresponding `refine ⟨…, trivial⟩` site needs the
trailing slot dropped as well. The `pverify_split_smt` tactic still
defensively strips trailing `True` goals so a user bundle that happens
to end in `True` doesn't break splitting, but the codegen no longer
relies on it. Pinned by [`Tests/Syntax/ObligationShape.lean`](Tests/Syntax/ObligationShape.lean).

### Every executable handler gets an obligation

The obligation generator emits a per-handler triple for **three**
handler shapes:

| Surface form                            | Handler def                  | Obligation form                                                              |
|-----------------------------------------|------------------------------|------------------------------------------------------------------------------|
| `on <ev> ([param : T]) { body }`        | `<M>.<S>.<ev>_handler`       | `(Inv ∧ DispatcherContract) ⇒ wp(markReceived lbl >>= handler, Inv)`         |
| `on <ev> goto <tgt>`                    | `<M>.<S>.<ev>_handler` (synth) | Same as above; the synthesised handler body is `goto this <tgt>_st .unit`. |
| `entry [(param : T)] { body }`          | `<M>.<S>.entry`              | `(Inv ∧ is_<M> this.ref s ∧ currentState = <S>_st) ⇒ wp(entry, Inv)`         |

The entry obligation has no `lbl` and no `markReceived` prelude —
entry doesn't dispatch on an event. The pre is conservative: it
verifies entry preserves invariants from *any* state with `this`
already in `<S>_st`, not only the one reached via a fresh allocation
or `goto`. When a future fix wires up the `InEntry` / `stage` runtime
gate, the pre can tighten.

Coverage is sound by construction: the user-directive pass and the
auto-default pass both iterate over `(machine, state) × (events ∪
{entry})`. Pinned by
[`Tests/Syntax/HandlerCoverage.lean`](Tests/Syntax/HandlerCoverage.lean)
(positive: the theorems exist; negative: a broken invariant on a goto
or entry is reported as disproved, not silently passed).

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
[`Tests/Syntax/PVerifyManualProof.lean`](Tests/Syntax/PVerifyManualProof.lean).
**Don't bake substantive automation into `#pverify` itself**;
that work belongs in user-callable tactics or `@[pverifyProof]`
theorems.

### Macro hygiene through `mkIdent`

`#gen_module` and `Syntax/Stmt.lean` emit identifiers that must
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

## Style guide

These rules apply to PLean source, proofs, and tactics. Most are
load-bearing: a violation can either silently waste solver time or
introduce a soundness gap. The bullets below tell you when each rule
applies, not just what it is.

### Don'ts

- **Don't over-comment.** Default to no comments. Add one only when the
  *why* is non-obvious (a hidden invariant, a workaround, a soundness
  consequence). Well-named identifiers explain *what*; don't restate
  them.
- **Don't reference phase numbers or plan-doc section IDs in source
  comments.** Plan docs evolve, source comments don't get re-numbered.
  If a comment needs to justify a design decision, state the reason
  directly.
- **Don't dump deliberation into comments.** No "I tried X but it
  didn't work because …", no decision logs, no exploratory narration.
  Document the *invariant the code maintains*, not the path you took
  to find it.
- **Don't cite other codebases in source comments.** OK to mention a
  specific PVerifier construct or a `Loom` API when the PLean piece
  exists to mirror or interface with it (e.g., "matches PVerifier's
  per-handler obligation shape"); not OK to gesture at "see paper X"
  or "similar to project Y". Citations belong in plan docs.
- **Don't trim `lake build` output on `#pverify` files.** A failing
  verification produces a structured report — failing-obligation names,
  copy-paste manual-proof skeletons, per-stage profile — *after* the
  `✖` line. Re-running the build to recover output is expensive (SMT
  solving dominates), so always read the full output the first time.
  Pipe to a file (`lake build Examples.Foo 2>&1 | tee build.log`) if
  it's long, and grep over the file rather than re-running.
- **Don't leave probe code lying around.** When a probe (a temporary
  `example`, `#check`, `#eval`, or a one-off file under `Tests/`) has
  served its purpose, either delete it or convert it into a real
  regression that pins a behaviour worth keeping. Don't pin trivial
  facts that follow directly from the surrounding code — pins are
  load-bearing only when they catch a real regression class.
- **Don't introduce paths or repo URLs in source comments.** Describe a
  module by its role (e.g., "the obligation generator", "the kind-guard
  injection pass"), not its filesystem location. Paths rot when files
  move.

### Do's

- **Correctness first.** A soundness regression is far worse than a
  performance regression or a verbose proof. Two soundness guards are
  pinned (`GlobalState`-shadowed binders, sorried `@[pverifyProof]`
  failure) by `Tests/Syntax/SoundnessRegression.lean` — don't regress
  them. Before adding a tactic that closes more goals, satisfy yourself
  that it cannot close *false* goals.
- **Decompose deliberately.** When a module crosses ~500 lines or
  serves more than one cohesive purpose, split it — but only when the
  split *reduces* coupling. A new sub-module that re-exports the same
  surface with extra import overhead is worse than the original. Match
  the decomposition to the existing `PLean/{Syntax,Commands,Verify,
  Semantics,Internal}/` axes where it fits.
- **Lean on `Verify/Tactic.lean` and extend it.** When a manual proof
  uses the same shape twice — `pverify_carry_after_recv`,
  `pverify_not_inflight[_by]`, `pverify_inflight_by`,
  `pverify_machine_has_type`, `pverify_split_smt` — that's the catalogue
  of "common pattern → atomic tactic". Before duplicating a proof shape,
  check whether a tactic already covers it; if a *new* recurring shape
  shows up, add it to the catalogue. The docstrings on each tactic
  spell out their calling pattern and prerequisites.
- **Shorten proofs after they close.** Once an obligation is green,
  cross-check it against neighbouring proofs. If the same boilerplate
  appears in 2+ places, factor it into a tactic and replace the
  inlined copies. Smaller proofs make future obligations easier to
  diagnose when SMT regresses.
- **Keep tactics atomic; compose for the macro shapes.** A tactic should
  do *one* recognisable proof step. If a pattern is "prove A, then use
  A to close B", implement two atomic tactics and a `tactic|`-macro
  that sequences them. Don't bake A+B into a single monolithic tactic
  that you can't reuse for "prove A but close C differently" later. The
  existing pverify_* family follows this rule — preserve it.
- **Tag new `GlobalState` update helpers with `@[pverifySimp]`.** SMT
  prep reduces them before lean-auto runs; an untagged helper reaches
  the solver as an opaque symbol and the obligation returns `unknown`.

## Common operations

### Add a new test or example

- **Regression**: drop the file under `Tests/Bootstrap/`,
  `Tests/Semantics/`, `Tests/Syntax/`, or `Tests/Verify/` — the
  `Tests` lean_lib globs `Tests.**`. A regression pins a behaviour
  (`#guard_msgs`-style) or covers a feature in isolation.
- **Protocol port / showcase**: drop it under `Examples/` — the
  `Examples` lean_lib globs `Examples.**`. Use this for substantial
  pmodules that demonstrate end-to-end verification of a real
  protocol.

### Iterate on a failing test

```bash
cd Src/PLean
lake build Tests.Syntax.MyTest 2>&1 | grep -E '^error:' -A 5
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
