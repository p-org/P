# PLean — Implementation Plan

A port of the P language and PVerifier into Lean 4, using **Loom** as the
verification backend. The goal is to keep P programs verifiable inside Lean,
without emitting UCLID5, while preserving PVerifier's per-handler modular proof
strategy.

## Design Decisions

1. **Shallow embedding.** A P machine is a Lean record of fields + a collection
   of Lean functions in the `PM` monad (the P monad — see
   [`Semantics/Monad.lean`](../PLean/Semantics/Monad.lean)). P states are dispatch
   tables of handlers. We do **not** build a deep `inductive PProgram`; deep
   embedding is only justified when we need to transform/analyze the program,
   and emitting proof obligations does not require it.

2. **Verification stays in Lean.** Verification conditions are dispatched to
   Loom's `loom_solve` (and ultimately to Z3/cvc5 via `lean-auto`). We do
   **not** emit UCLID5. Choice between Loom and lean-smt as the SMT bridge is
   deferred; Loom is the default because velvet already uses it.

3. **One Hoare triple per handler.** Mirroring PVerifier's UCLID5 strategy
   ([`Uclid5CodeGenerator.cs:1432-1591`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L1432-L1591)),
   each `(Machine, State, Event)` handler gets a single Lean lemma asserting
   `triple (Inv ∧ guard) handler (fun _ => Inv)`. The `pverify` command walks
   the registry and synthesizes these lemmas.

4. **Registry, not AST.** The only "AST-like" thing we keep is a small Lean
   environment extension that records per-machine metadata (machine name,
   states, observed events, invariant references). This is the same trick
   velvet uses with `velvetObligations`. The runtime artifacts are ordinary
   Lean defs.

5. **Bridge to existing P infrastructure (PChecker, PEx) is the LAST goal.**
   Not an early architectural concern. Eventually realized by a deep-AST
   projection from the registry that emits `.p` source.

## Architecture Overview

A P machine like:

```p
machine Server receives ePing sends ePong {
  start state Idle {
    on ePing do (req: tPing) {
      send req.client, ePong, (id = req.id,);
    }
  }
}
```

elaborates to (sketch):

```lean
namespace PServer
  structure Fields where  -- machine fields → record
  def Idle_ePing (self : MachineRef) (req : TPing) : PM Unit := do
    send req.client ePong { id := req.id }
end PServer
```

`pverify` then synthesizes (per handler):

```lean
@[loomSpec] lemma Server_Idle_ePing_obligation
    (self : MachineRef) (lbl : Label) (req : TPing) :
    triple
      (PInv_All ∧ inflight lbl ∧ lbl targets self
       ∧ stateOf self = .Idle ∧ lbl is ePing ∧ payloadOf lbl = req)
      (PServer.Idle_ePing self req)
      (fun _ => PInv_All) := by
  pverify  -- = unfold handler defs, then loom_solve
```

The triple shape mirrors `Uclid5CodeGenerator.cs:1520-1591` exactly — same
`requires`, same `ensures` — but stays inside Lean.

## Module Layout

```
Src/PLean/
  lakefile.lean                  # require Loom (same revision velvet uses)
  lean-toolchain                 # match Loom's
  PLean.lean                     # top-level facade

  PLean/
    Semantics/
      GlobalState.lean           # record { sent, received, machines, actionCount }
      Label.lean                 # Label = (target, EventOrGoto, actionCount); Event/Goto sum
      Monad.lean                 # PM α := StateT GlobalState (NonDetT DivM) α
                                 # (the "P monad"); MAlgOrdered instance
      Primitives.lean            # send/raise/goto/new/announce as PM combinators
                                 # (Lean functions in PM, mirroring Uclid5CodeGenerator's
                                 # statement-emission cases)
      Predicates.lean            # `inflight`, `sent`, `is`, `targets` as Prop
      Default.lean               # sanity invariants (UniqueActions, IncreasingCount,
                                 #                    ReceivedSubsetSent)

    Surface/
      Registry.lean              # env extension: per-machine metadata + obligation list
      Notation.lean              # ==>, <==>, `is`, `targets`, `inflight`, `sent`, quantifiers
      Machine.lean               # `machine`, `state`, `entry`, `on _ do`, `on _ goto`, `spec`
      Events.lean                # `event`, `eventset`, `enum`, `type`, `interface`
      Verify.lean                # `invariant`, `axiom`, `init`, `pure`, `param`,
                                 #   `lemma`/`theorem` group
      Proof.lean                 # `proof { prove ... using ... except ... }`, `prove_correct`
      Stmt.lean                  # statement-level macros → PM combinators
      ForeignFun.lean            # `fun … requires … ensures …` (uninterpreted + spec)

    Verify/
      Obligation.lean            # generates per-(M,S,E) Hoare triples from registry
      Tactic.lean                # `pverify`: unfold handler, call loom_solve
      Sanity.lean                # auto-generates default obligations (PVerifier `default`)

    Examples/                    # ported tutorials, end-to-end

  Tests/
    Semantics/                   # PM combinator unit tests + small Hoare-triple proofs
    Verify/                      # known-good handler obligations (regression)
```

## Phased Plan

### Phase 0 — Bootstrap (≈2 days)
- [ ] [`Src/PLean/lakefile.lean`](../lakefile.lean) requiring Loom at velvet's pinned commit
- [ ] [`Src/PLean/lean-toolchain`](../lean-toolchain) matching Loom
- [ ] CI job (mirror [`.github/workflows/ubuntuci.yml`](../../../.github/workflows/ubuntuci.yml))
- [ ] Empty [`PLean.lean`](../PLean.lean) builds with `lake build`

> Phase 0 has been expanded into its own document: see
> [`PLAN_P0.md`](PLAN_P0.md) for the full bootstrap breakdown, including
> multi-file aggregation, the `pmodule` surface, env extensions, and the
> three-file PingPong demo. The Phase-0 section here remains the at-a-glance
> entry point; the detailed plan lives in `PLAN_P0.md`.

### Phase 1 — Semantic core (≈1 week)
- [ ] [`Semantics/GlobalState.lean`](../PLean/Semantics/GlobalState.lean):
      record matching `Uclid5CodeGenerator.cs:594-606` shape
- [ ] [`Semantics/Label.lean`](../PLean/Semantics/Label.lean): `Label`, `EventOrGoto`
- [ ] [`Semantics/Monad.lean`](../PLean/Semantics/Monad.lean):
      `PM α := StateT GlobalState (NonDetT DivM) α`; derive `MAlgOrdered` by composition
- [ ] [`Semantics/Primitives.lean`](../PLean/Semantics/Primitives.lean):
      `send`, `raise`, `goto`, `new`, `announce` updating `GlobalState` exactly as
      `Uclid5CodeGenerator.cs:1981-1999`
- [ ] [`Semantics/Predicates.lean`](../PLean/Semantics/Predicates.lean):
      `inflight`, `sent`, `is`, `targets`
      (names match the P surface keywords from
      [`PLexer.g4:72`](../../PCompiler/CompilerCore/Parser/PLexer.g4#L72) —
      not C# AST node names like `FlyingExpr`)
- [ ] [`Semantics/Default.lean`](../PLean/Semantics/Default.lean):
      three sanity invariants
- [ ] **Exit criterion**: hand-write a 2-state ping-pong machine purely as Lean
      defs (no surface syntax), state a user invariant, prove all four handler
      triples via `loom_solve`

### Phase 2 — Registry + minimal surface (≈1 week)
- [ ] [`Surface/Registry.lean`](../PLean/Surface/Registry.lean): env extension
- [ ] [`Surface/Events.lean`](../PLean/Surface/Events.lean):
      `event`, `eventset`, `enum`, `type`, `interface` commands
- [ ] [`Surface/Machine.lean`](../PLean/Surface/Machine.lean):
      `machine`, `state`, `entry`, `on…do`, `on…goto`
- [ ] [`Surface/Stmt.lean`](../PLean/Surface/Stmt.lean):
      statement macros for `send`, `raise`, `goto`, `assign`
- [ ] **Exit criterion**: rewrite the Phase-1 ping-pong example in surface
      syntax; still verifies

### Phase 3 — Verification declarations (≈4 days)
- [ ] [`Surface/Verify.lean`](../PLean/Surface/Verify.lean):
      `invariant`, `axiom`, `init`, `pure`, `lemma`/`theorem` groups
- [ ] [`Surface/Proof.lean`](../PLean/Surface/Proof.lean):
      `proof { prove G using P except E; }`, `prove_correct`
- [ ] [`Verify/Obligation.lean`](../PLean/Verify/Obligation.lean):
      walk registry → synthesize per-handler `@[loomSpec]` lemmas
- [ ] [`Verify/Tactic.lean`](../PLean/Verify/Tactic.lean): `pverify` tactic
- [ ] [`Verify/Sanity.lean`](../PLean/Verify/Sanity.lean): default obligations

### Phase 4 — Spec machines (≈3–4 days)
- [ ] Flatten spec machines to global vars + handler procedures
      (mirrors [`Uclid5CodeGenerator.cs:980-1088`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L980-L1088))
- [ ] Hook spec handlers at every `send` of an observed event

### Phase 5 — Remaining surface (≈1 week)
- [ ] Quantifier notations
- [ ] `foreach … invariant …`
- [ ] [`Surface/ForeignFun.lean`](../PLean/Surface/ForeignFun.lean):
      `pure` (with body → `def`, without → `opaque`), foreign-fun
      `requires`/`ensures`, `param`
- [ ] Polish error messages

### Phase 6 — Tutorial port (≈1–2 weeks)
- [ ] Port [`Tutorial/1_ClientServer/`](../../../Tutorial/1_ClientServer/) into PLean
- [ ] Port [`Tutorial/2_TwoPhaseCommit/`](../../../Tutorial/2_TwoPhaseCommit/) into PLean
- [ ] Compare wall time vs PVerifier+UCLID5 on the same examples

### Phase 7 — Stretch / future
- [ ] PChecker bridge: deep-AST projection from registry → `.p` source → shell
      out to `p check`
- [ ] Evaluate lean-smt as an alternative to Loom
- [ ] Counter-example surfacing

## Open Design Problems

### Temporal predicate: a precedence operator `≺` over events

PVerifier today can only state safety properties over the *current* state
([`Uclid5CodeGenerator.cs:594-606`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L594-L606)
— `GlobalState` exposes `sent` / `received` as sets of labels but exposes
no ordering between them). State-based existence claims like
"some `ePing` was sent and some `ePong` was sent" are already expressible:

```p
∀ (lbl : event), sent lbl ∧ lbl is ePong → ∃ (p : event), sent p ∧ p is ePing
```

But this is **not actually temporal** — it doesn't constrain the order of
the two sends. Genuinely temporal properties — "every `ePong` is a response
to a *prior* `ePing`", "every `commit` was preceded by a `prepare` from
every participant" — require an ordering relation between events. This is
what's missing, and what PLean treats as a first-class improvement over
PVerifier.

**The missing primitive: `≺ : Label → Label → Prop`.** A single binary
precedence operator on events ("`a ≺ b`" reads "`a` was sent before `b`").
With `≺` plus the existing `is`/`sent`/`inflight`/quantifiers, the
PingPong invariant becomes genuinely temporal:

```p
∀ (lbl : event), sent lbl ∧ lbl is ePong →
  ∃ (p : event), sent p ∧ p is ePing ∧ p ≺ lbl
```

This is sufficient for the safety properties in P's tutorial protocols
(ClientServer, TwoPhaseCommit, FailureDetector). Full LTL — `prev`,
`since`, `eventually`, `always` — is **out of scope for v1**. We are
adding *one* binary operator, not a temporal logic.

**The encoding problem.** State-based verification needs a state-resident
witness. There are two plausible implementations of `≺`; we pick one
before Phase 3 obligation generation solidifies. Both are invisible to
the user — they only differ in `GlobalState` shape and SMT performance.

1. **`≺` from `actionCount` (recommended).** PVerifier already increments
   a global
   [`actionCount`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L768-L771)
   on every `send`/`goto` and stores it on each `Label`
   ([`Uclid5CodeGenerator.cs:760-792`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L760-L792)).
   Define `a ≺ b := a.actionCount < b.actionCount`. Cost: zero — this
   reuses machinery that already ships in PVerifier. SMT sees a plain
   integer comparison and stays in linear arithmetic.

2. **`≺` from event history.** Add a ghost field
   `eventHistory : List Label` to `GlobalState`, append on every `send`,
   and define `a ≺ b := indexOf a < indexOf b` in the history. More
   general — allows reasoning about repeated occurrences of the same
   label — but SMT now sees an unbounded list, which `loom_solve` may
   not discharge cleanly.

**Recommended sequencing.**
- **Phase 1**: commit to encoding (1). It's a strict superset of what
  PVerifier already does and bakes nothing new into `GlobalState` —
  `actionCount` is already there. Define `≺` as a Lean `def` over Labels.
- **Phase 2** (surface): declare the notation
  `notation:50 a " ≺ " b => a.actionCount < b.actionCount` in
  [`Surface/Notation.lean`](../PLean/Surface/Notation.lean), and register
  the `\prec` input shortcut. ASCII fallback: `<<` if users prefer (TBD).
- **Phase 3** (verification): no new obligation-generator work — `≺`
  reduces to integer comparison, which `loom_solve` already handles.

**Out of scope for v1** (track as forward-looking in
[`STATUS.md`](STATUS.md) "Anticipated" risks):
- Full LTL (`prev`, `since`, `eventually`, `always`) — these would
  desugar to quantifications over `actionCount`, but the macro
  surface and decision procedures are non-trivial. Reconsider if v1
  examples turn out to need more than `≺`.
- Liveness ("every request eventually gets a response") needs fairness
  assumptions — not in scope.
- Refinement (`test … refines …` from
  [`PParser.g4:291`](../../PCompiler/CompilerCore/Parser/PParser.g4#L291))
  is a different kind of temporal claim — not in scope.

## Risks

- **MAlgOrdered for `StateT GlobalState (NonDetT DivM)`.** Loom has instances
  for individual layers; the composition may need a bespoke proof. Spike this
  early in Phase 1 — if it's harder than expected, project velocity drops.

- **Helper-function unfolding.** Handlers themselves are passively dispatched
  by the runtime and never call each other, so there is no handler-to-handler
  ordering problem. But handler bodies do call ordinary `fun` decls. PVerifier
  resolves this trivially by marking functions `procedure [inline]`
  ([`Uclid5CodeGenerator.cs:1347`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L1347),
  [`:1386`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L1386))
  — UCLID5 sees the fully-inlined body, no specs needed. PLean should do the
  same for v1: `pverify` unfolds helper defs before calling `loom_solve`. Only
  if we later want compositional helper verification (foreign `fun` with
  `requires`/`ensures`, or summarised pure functions) do we need
  `@[loomSpec]`-driven ordering. Phase 3 for the unfolding decision; ordering
  deferred to Phase 5.

- **Map/seq SMT encoding parity.** PVerifier hand-encodes maps as
  `[K]Option V` so cvc5 stays in decidable theories. PLean must use the same
  encoding or proofs that pass PVerifier may stall in PLean.

- **Foreign types / uninterpreted symbols.** P's `type T;` and bodyless foreign
  `fun` map to UCLID5 `type T;` and uninterpreted procedures. Loom needs the
  analog — likely opaque `axiom`/`opaque` decls. Verify Loom doesn't choke on
  uninterpreted symbols in goals.

- **SMT scaling.** Loom is sequential per goal; PVerifier parallelizes across
  files. For Tutorial/5_Paxos/ this could be untenable. Not blocking for v1
  (which targets ClientServer + TwoPhaseCommit).

## First Deliverable (≈1.5 weeks, end of Phase 1)

A single hand-written Lean file with no macros that:
- defines `GlobalState`, `PM`, `send`, the three default invariants
- hand-codes a 2-state ping-pong machine as a Lean record + four defs
  (Idle_entry, Idle_ePing, Active_entry, Active_ePong)
- states the safety invariant "every ePong is in response to a sent ePing"
- proves all four handler triples via `loom_solve`

This validates the whole pipeline before any macro work. The macros in
Phases 2–3 then have a concrete target to elaborate into.

## References

- PVerifier UCLID5 backend:
  [`Src/PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs)
- P parser grammar (verification syntax):
  [`Src/PCompiler/CompilerCore/Parser/PParser.g4`](../../PCompiler/CompilerCore/Parser/PParser.g4)
- velvet (reference for shallow-embedded Lean DSL on Loom): `~/Downloads/velvet`
- Loom (verification backend): `~/Downloads/loom`
