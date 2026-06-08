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
`#pverify M` will (Phase 3) generate and discharge per-handler
Hoare-triple obligations against `loom_solve`-equivalent automation
that PLean owns.

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
    Verify.lean             -- invariant/paxiom/init-holds/function/pinstance
    Notation.lean           -- ≺, is, targets notations
  Commands/
    GenModule.lean          -- #gen_module M (synthesises Sig, emits handlers)
    PWf.lean                -- #pwf M (well-formedness)
    PVerify.lean            -- #pverify M (Phase 3 will own obligation gen)
    PrintModule.lean        -- #print_pmodule M (debug)

Examples/
  PingPong/                 -- the canonical surface demo

Tests/
  Bootstrap/                -- Phase-0 regressions: registration, errors, multi-file
  Semantics/                -- Phase-1 regressions:
                               StackSpike (instance synthesis)
                               HandPingPong (M1 — hand-written triples)
                               Combinators (.run-based primitive tests)
                               SmtRoundtrip (cvc5 wiring)
  Surface/                  -- Phase-2 regressions:
                               Phase2PingPong (M2 — surface triples)
                               Combinators (surface .run-based tests)
```

## Phase-by-phase planning docs (READ THESE FIRST)

Every substantive change should consult the relevant plan doc. They
are **the authoritative design record** — STATUS.md is a living
tracker, not a design doc.

- [`docs/PLAN.md`](docs/PLAN.md) — overall plan; phase checkboxes
- [`docs/PLAN_P0.md`](docs/PLAN_P0.md) — Phase 0 (Bootstrap)
- [`docs/PLAN_P1.md`](docs/PLAN_P1.md) — Phase 1 (Semantic core)
- [`docs/PLAN_P2.md`](docs/PLAN_P2.md) — Phase 2 (Registry + surface);
  decisions D8–D17, risks R8–R14
- [`docs/PLAN_P3.md`](docs/PLAN_P3.md) — Phase 3 (Verification
  declarations); decisions D18–D28, risks R15–R21. Names the
  Tutorial/Advanced benchmarks that drive M3 acceptance.
- [`docs/STATUS.md`](docs/STATUS.md) — phase status, decision log,
  milestones, anticipated risks

When PLAN.md and a phase plan disagree, the phase plan wins. PLAN.md
predates the implementation; PLAN_P{0..3} were written against the
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

## Phase status (as of 2026-06-05)

- Phase 0 (Bootstrap) — ☑ M0 reached.
- Phase 1 (Semantic core) — ☑ M1 reached. Hand-written ping-pong
  verifies via `wpgen` + manual proof tail.
- Phase 2 (Registry + minimal surface) — ☑ M2 reached.
  `#gen_module` synthesises per-pmodule `Sig`/`PM'`/`GS`; surface
  macros target the real PM; M2 surface ping-pong verifies in
  `Tests/Surface/Phase2PingPong.lean`. `Internal/Stub.lean` is
  deleted.
- Phase 3 (Verification declarations) — ☐ next. Plan in PLAN_P3.

## Conventions worth knowing

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
PLean's own `pverify` tactic (Phase 3, D22 in PLAN_P3) recomposes
the underlying pieces (`wpgen` + `loom_intro` chain + grind/SMT
fallback) without the `WithName` scaffolding.

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

### `is` notation is registry-aware

`Surface/Notation.lean` has `lbl is <ev>` as a *macro* (not a plain
notation) that rewrites to `is_<ev> lbl`. Phase 3 (D20) extends it
to also recognise machine-kind RHS (`m is Server` → kind check).
Don't treat `is` as if it were `Eq` — for events it's a
ctor-tag check (no payload equality) per P semantics.

### `MachineRef` stays flat

`MachineRef := Nat`. Per-machine *static* type distinction lives in
the wrapper structs (D10): `structure Server where ref : MachineRef`
plus `instance : Coe Server MachineRef`. The runtime carrier (state
map, label target field) is keyed on `MachineRef`. A per-kind
*dynamic* check (`m is Server` for `m : MachineRef`) is Phase 3's
D20 — extends `MachineState` with an `Option MachineKindTag` and
synthesises `<Mod>.MKind`. Don't introduce a `MachineRef Server`-
parameterised refinement; it would diverge from PVerifier's flat
encoding.

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

`Internal/Stub.lean` was deleted at end of Phase 2 (decision D15);
any hit is a regression.

### Run cvc5 / z3 from Lean

The solvers are downloaded into Loom's build dir on first build. To
verify they're reachable:

```bash
lake build Tests.Semantics.SmtRoundtrip
# expect: "Goal proven by cvc5. Trusting SMT solver result."
```
