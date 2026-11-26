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
    Loop.lean        -- pforeach primitive + @[loomSpec] WPGen.pforeach
  Syntax/
    Module.lean      -- pmodule M ... end M
    Types.lean       -- type N / enum N / type N = (...)
    Events.lean      -- event ev : T
    Machine.lean     -- machine M { var ...; state S { ... } }
    Stmt.lean        -- send / raise / goto / announce / var-assign macros
    Loop.lean        -- foreach / while macros + loop-invariant clauses
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

Examples/            -- protocol case studies (see Examples/ index)
Tests/               -- regressions; Tests.** is globbed
```

## Contributor docs

- [`docs/ROADMAP.md`](docs/ROADMAP.md) — collaborator-facing entry
  point; current phase, workstreams, sprint allocation.
- [`docs/STATUS.md`](docs/STATUS.md) — phase status, decision log,
  milestones.
- [`docs/ProofSkill.md`](docs/ProofSkill.md) — practical workflow for
  finding inductive invariants, writing manual proofs, and coping with
  SMT complexity (higher-order rejection, bundle sizing, `using` chains).

## Verification benchmarks

Phase 3+ targets the verified benchmarks under
[`Tutorial/Advanced/`](../../Tutorial/Advanced/) (parent repo).

- **M3 (Phase 3)**: `6_DistributedLock`, `8_LockServer`,
  `3_RingLeaderVerification` — basic Theorem/Proof/Lemma blocks,
  machine-kind `is`, multi-Lemma `using` chains.
- **Phase 4 (specs)**: `1_ChainReplicationVerification`.
- **Phase 5 (foreach/maps)**: `5_Consensus`,
  `2_TwoPhaseCommitVerification`, `7_ShardedKV`. Maps + `foreach`
  shipped (the `ShardedKV` port verifies); `Consensus` / 2PC blocked
  on a stronger loop-aware `default_inv` so the auto-emitted `prove
  default;` obligations under loops close without a hand-written
  `DefaultInvariants` loop invariant.
- **Phase 6 stretch**: `4_Paxos`.

Most Tutorial/Advanced benchmarks need surface features that aren't
built yet — check `docs/ROADMAP.md`'s workstream breakdown before
attempting one.

## Phase status

See [`docs/STATUS.md`](docs/STATUS.md) for the current closure
snapshot and [`docs/ROADMAP.md`](docs/ROADMAP.md) for the workstream
breakdown. Headline: Phases 0–3 complete; Phase 4 (spec machines) is
next critical path; Phase 5 (remaining surface) is partial (`assume`
and loop-aware `default_inv` pending).

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
| `foreach (x in xs) inv N : I; { … }` | same surface, identifier-only invariant names |
| `while (c) inv N : I; [done_with …;] [decreasing …;] { … }` | same surface, optional `done_with` / `decreasing` clauses |

`Lemma`, `Theorem`, `Proof`, `prove`, `using`, `system` are
new-in-PLean keywords for the verification-declaration surface
(Phase 3); `default` is a reserved sentinel inside `Proof` blocks.

`PLean.choose : Int → PM Int` returns a nondeterministically-chosen
`Int` in `[0, bound]`. Implemented via `MonadNonDet.pickSuchThat`;
the WP spec (registered as `@[loomSpec]`) gives the verifier `0 ≤ x
∧ x ≤ bound` as a hypothesis at the call site. See
[`Examples/ClockBound.lean`](Examples/ClockBound.lean) for usage.

### Container types

Surface macros: `set[T]` → `Set T`, `map[K, V]` → `K → Option V`,
`seq[T]` → `List T` (no SMT), `option[T]` → `Option T`. Mutation
macros: `s += (e)`, `s -= (e)`, `m[k] = v`, `m[k] += (e)`,
`m[k] -= (e)`. Lookup-after-mutation lemmas are tagged
`@[pverifySimp]` so SMT prep reduces post-state lookups directly.
See [`Examples/ShardedKV.lean`](Examples/ShardedKV.lean).

### Loops (`foreach` / `while`)

`foreach (x in xs) invariant N : I ; { body }` and `while (cond)
invariant N : I ; [done_with …;] [decreasing …;] { body }`. Both
desugar to a rigid gadget chain that `wpgen` matches — inserting
extra `do`-statements between gadgets or omitting one collapses the
match. `done_with` defaults to `¬cond` for `while`; `decreasing`
defaults to `none` (informational only under partial correctness).

**Limitation.** Auto-emitted `prove default;` under a loop-bearing
handler can disprove when the loop invariant is too weak to entail
`DefaultInvariants` for the post-state. Strengthen the loop
invariant, or write a manual `@[pverifyProof]`. See
[`Tests/Syntax/Loop.lean`](Tests/Syntax/Loop.lean).

## Conventions worth knowing

Source comments cover the *why* in detail. The points below are the
load-bearing invariants you need to respect when editing.

- **`system <σ>` declares the state binder for a pmodule.** Every
  subsequent `invariant` / `init-holds` / `paxiom` body may reference
  `σ` as the live state; the materialiser binds it as the lambda
  argument. A pmodule without `system <σ>` emits state-independent
  clauses. Soundness guard (`rejectStateShadowIn`, pinned by
  `Tests/Syntax/SoundnessRegression.lean`) rejects bodies that name
  `GlobalState` in any binder.
- **Field-projection sugar.** Inside state-bound clauses, `n.<v>`
  desugars to `(σ.machines n.ref).fields.<M>_<v>` and `e.<f>` to
  `(<ev>_payload_of e).<f>`. Gated on registered field names; runs
  before kind-guard injection so it sees the original quantifier
  types.
- **Kind guards auto-injected on quantifiers.** `∀ n : <M>, body`
  becomes `∀ n : <M>, is_<M> n.ref s → body` (and similarly for `∃`
  and event quantifiers). Skipped when the body already mentions the
  guard. Pinned by `Tests/Syntax/SoundnessRegression.lean`.
- **Container `var`s are hoisted into `Containers`.** `set[T]` /
  `map[K, V]` vars hoist out of `Fields` into a per-pmodule
  `Containers` struct uncurried with `MachineRef`, so `n.<v>`
  reduces to a flat applied symbol lean-auto can translate.
  `seq[T]` and first-order vars stay in `Fields`. Mirrors
  PVerifier's UCLID5 2D-array layout. Exercised by
  [`Examples/ShardedKV`](Examples/ShardedKV.lean).
- **`MachineRef := Nat`; per-machine type is a wrapper.** Dynamic
  kind check (`<M>_allocated`, public alias `is_<M>`) goes through
  a `Nat` kind tag plus `currentState ∈ <M>'s states` (load-bearing
  — without it spurious models fabricate a kind/state mismatch).
  Don't introduce a `MachineRef <M>` refinement.
- **Bundle conjunctions don't carry trailing `True`.**
  `buildConjAt` and `emitConjPredicate` emit `p1 ∧ … ∧ pn` with no
  terminator. Empty bundles collapse to `fun _ => True`. Manual
  `@[pverifyProof]` statements must match exactly. Pinned by
  `Tests/Syntax/ObligationShape.lean`.
- **Every executable handler gets an obligation.** Three shapes:
  `on <ev> { body }`, `on <ev> goto <tgt>`, and `entry { body }`.
  Coverage is sound by construction — the user-directive pass and
  the auto-default pass iterate over `(machine, state) × (events
  ∪ {entry})`. Pinned by `Tests/Syntax/HandlerCoverage.lean`.
- **Per-handler triples are pure consecution; init is separate.**
  A per-handler obligation is `(Inv ∧ DispatcherContract) ⇒
  wp(handler, Inv)`; `InitConditions` discharges via separate
  base-case VCs (one per individual invariant per `prove` target).
  Premises (`using P`) don't get base VCs.
- **`using` premises must be `prove`d.** `prove safety using
  framework ;` requires another directive `prove framework ;`
  whose obligations actually discharge. Mirrors PVerifier's
  `MarkProvenInvariants` + `ShowRemainings`. Failures surface as
  `[missing premise]` records. `default` is treated as a sibling
  flag: `using default` needs a `prove default ;` whose default
  obligations all close. Pinned by
  `Tests/Syntax/MissingPremise.lean`.
- **`#pverify` is an SMT-discharge command, not a tactic engine.**
  For each obligation it consults `@[pverifyProof]`, emits a
  theorem with a `pverify` tail, and inspects the value for
  `sorry`. Don't bake substantive automation into `#pverify`
  itself; that belongs in user-callable tactics or
  `@[pverifyProof]` theorems.
- **Macro hygiene through `mkIdent`.** Identifiers that resolve
  against user-namespace constants (`Sig`, `E`, `G`, `this`,
  `<S>_st`, …) must be built via `mkIdent` and spliced. Bare names
  inside `` `(...) `` quotations get hygiene marks and fail to
  resolve. Symptom: `PLean.DivM✝` or `Sig✝` in error output.
- **`loom_solve` is CaseStudies-only.** PLean uses `wpgen` + simp
  set + Veil-style preprocessing + `loom_smt`; see
  `Verify/Tactic.lean`.
- **Tag new state-update helpers `@[pverifySimp]`.** The pre-SMT
  simp pass uses this set; an untagged helper reaches lean-auto
  as an opaque symbol and the obligation returns `unknown`.
- **Counter-examples are decoded, not dumped.** A disproved
  obligation routes its model through `Verify/CexParse.lean` +
  `CexModel.lean` into a per-machine state table, `sent` trace,
  `containers:` section, and witnesses. Don't pin exact model
  *values* in tests — pin de-mangling and structural markers (see
  `Tests/Verify/CexEndToEnd.lean`).

## Style guide

See [`docs/STYLE.md`](docs/STYLE.md) for the source/proof/tactic
style rules. Soundness rule of thumb: before adding a tactic that
closes more goals, satisfy yourself it can't close *false* goals.
Two pinned soundness guards live in
`Tests/Syntax/SoundnessRegression.lean` — don't regress them.

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
