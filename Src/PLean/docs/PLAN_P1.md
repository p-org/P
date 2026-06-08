# PLean — Phase 1 (Semantic core) Plan

This document expands the Phase-1 entry in [`PLAN.md`](PLAN.md). The objective
of Phase 1 is to **replace the stub `PM := Id` with a real semantic core** —
`GlobalState`, the `PM` monad over Loom, the `send`/`goto`/`raise` primitives,
the state predicates, and the three default invariants — and to prove the first
real verification milestone (**M1**): a hand-written, macro-free ping-pong file
whose four handler Hoare triples discharge automatically.

Phase 1 is where the project stops being a parser/registry and starts being a
verifier. Everything downstream (Phase 2 surface, Phase 3 obligation
generation) elaborates into the artifacts built here.

> **Read this first.** Phase 1 was scoped in [`PLAN.md`](PLAN.md) against a
> version of the Loom dependency that no longer exists at our pinned revision.
> The section ["What changed since PLAN.md"](#what-changed-since-planmd-was-written)
> documents the deviations and the evidence for each. The rest of the plan is
> written against the dependency as it actually ships today.
>
> **On Loom source citations.** Loom lives in PLean's build tree as a vendored
> dependency, not in this repo, so its files are not linkable from here. This
> doc therefore cites Loom by *module path + definition name* in plain text
> (e.g. `WPGen.forWithInvariantLoop` in `Loom/MonadAlgebras/NonDetT/Basic.lean`)
> and inlines the substance of what was read, so the plan stands on its own.
> Those paths are relative to the Loom package root (public repo
> `verse-lab/loom`, pinned at the revision in [`lakefile.lean`](../lakefile.lean)).
> In-repo references (PVerifier, PLean's own source) remain clickable links.

---

## What changed since PLAN.md was written

We pin Loom at the revision recorded in [`lakefile.lean`](../lakefile.lean)
(`d10340821daf…`). Three facts about that revision reshape Phase 1. All were
verified by reading the vendored Loom source while planning this phase.

### 1. Velvet was deleted; **Cashmere** is the only surviving reference DSL

The pinned commit is literally titled **"Remove velvet (#43)"**. PLAN.md cites
Velvet ~5 times as the precedent for "shallow-embedded Lean DSL on Loom"
(`velvetObligations`, "the same trick velvet uses", the `prove_correct` flow).
**None of that code is present at our pin.** What remains is the **Cashmere**
case study (`CaseStudies/Cashmere/Cashmere.lean` and `Syntax_Cashmere.lean` in
the Loom package).

Cashmere is now our template for *every* Phase-1 pattern: monad-stack
construction, the `MAlgOrdered` derivation, the Hoare-triple shape, and the
`prove_correct`/`loom_solve` flow. Where this plan says "mirror the reference,"
it means Cashmere, not Velvet.

> The `velvetObligations` / `VelvetObligation` env-extension scaffold that
> PLAN.md and STATUS.md reference *does* still exist, but in Loom's
> `CaseStudies/Extension.lean` — see finding #3 for why that matters.

### 2. The `MAlgOrdered` composition "spike" is essentially pre-resolved — and the layer order in PLAN.md is likely wrong

PLAN.md proposes `PM α := StateT GlobalState (NonDetT DivM) α` and flags the
`MAlgOrdered` composition as the #1 risk to spike before committing. Cashmere
answers most of this for free. Its monad is:

```lean
abbrev CashmereM := NonDetT (ExceptT String (StateT Bal DivM))
-- "all the necessary instances ... are generated automatically, including
--  μ - Ordered Monad Algebra instance for CashmereM"   (verbatim comment)
```

The composing instances all ship in the **`Loom`** library (not CaseStudies):

| Layer | `MAlgOrdered` instance | `MAlgLift` instance | Loom source (module · def) |
|---|---|---|---|
| `Id` / `DivM` base | `MAlgOrdered DivM Prop` (scoped) | — | `Instances/Basic.lean` |
| `StateT σ` | `MAlgOrdered (StateT σ m) (σ → l)` | `MAlgLift m l (StateT σ m) (σ→l)` | `Instances/StateT.lean` |
| `ExceptT ε` | `MAlgOrdered (ExceptT ε m) l` | yes | `Instances/ExceptT.lean` |
| `NonDetT` | `MAlgOrdered (NonDetT m) l` (scoped) | `MAlgLift m l (NonDetT m) l` | `NonDetT/Basic.lean` |

…and they chain via the `MAlgLiftTTrans` instance in
`Loom/MonadAlgebras/Defs.lean`.

**Two consequences:**

- **The layer order should follow Cashmere: `NonDetT` *outermost*, not
  innermost.** PLAN.md's `StateT GlobalState (NonDetT DivM)` puts `NonDetT`
  inside `StateT`. Every shipping example, the loop VC generator
  (`WPGen.forWithInvariantLoop` in `Loom/MonadAlgebras/NonDetT/Basic.lean` is
  defined for `forIn (m := NonDetT m)`), and the `MonadLift m (NonDetT m)`
  direction (same file) assume `NonDetT` sits on top. The recommended PLean
  stack is therefore:

  ```lean
  abbrev [PM](../PLean/Semantics/Monad.lean#L38-L39) (α : Type) := NonDetT (StateT GlobalState DivM) α    -- assertion lattice: GlobalState → Prop
  ```

  (We drop Cashmere's `ExceptT String` layer — P-handler safety verification
  has no exception effect in v1. `raise halt` and `assert` are modeled in the
  predicate/obligation layer, not as monadic exceptions.) Both orderings happen
  to yield the same top-level assertion lattice `GlobalState → Prop`, so the
  choice is about *which configuration Loom's tactics are tested against* — and
  that is unambiguously `NonDetT`-outermost. **Task 1 confirms this empirically
  and locks the decision.**

- **The `MAlgOrdered DivM Prop` and `MAlgOrdered (NonDetT m) l` instances are
  `scoped`** inside `PartialCorrectness.DemonicChoice` (and the `Total`/`Angelic`
  variants). They are invisible until you `open PartialCorrectness DemonicChoice`
  or set `set_option loom.semantics.termination "partial"` +
  `loom.semantics.choice "demonic"` (which open the scopes as a side effect —
  the `set_option` elaborators in `Loom/MonadAlgebras/WP/Options.lean` do this).
  **PLean's v1 default is partial-correctness + demonic choice** (demonic =
  "the property must hold for every nondeterministic schedule," which is exactly
  what safety verification wants). Phase 1 bakes this `open` into the semantics
  facade so downstream files don't repeat it.

### 3. `loom_solve` is **not** in the library PLean depends on

This is the biggest newly-surfaced gap against PLAN.md's exit criterion ("prove
all four handler triples via `loom_solve`").

PLean's [`lakefile.lean`](../lakefile.lean) requires the Loom **package** but
only globs the `Loom` lean_lib. The `loom_solve` / `loom_solver` tactics, the
`bdef` / `prove_correct` commands, and the `velvetObligations` scaffold all live
in the **`CaseStudies`** lib:

- `loom_solve`, `loom_solver` → `CaseStudies/Tactic.lean`
- `bdef`, `prove_correct`, `VelvetObligation` → `CaseStudies/Cashmere/Syntax_Cashmere.lean`,
  `CaseStudies/Extension.lean`

What **is** reachable from a bare `import Loom.*` (the building blocks
`loom_solve` is *composed from*), all under `Loom/MonadAlgebras/`:

| Primitive | Purpose | Loom source (module · def) |
|---|---|---|
| `triple`, `wp`, `spec` | Hoare-triple + WP definitions | `WP/Basic.lean` (`triple`, `wp`) |
| `wpgen`, `wpgen_step`, `mwp` | weakest-precondition goal generation | `WP/Gen.lean` (`wpgen`) |
| `loom_intro`, `loom_split` | hypothesis introduction with assertion names | `WP/Tactic.lean` |
| `loom_smt [hints]` | shell out to z3/cvc5 via lean-auto | `Loom/SMT.lean` (`loom_smt`) |
| `WPGen`, `loomSpec`, `invariantGadget` | obligation/attribute machinery | `WP/Gen.lean`, `WP/Attr.lean` |

**Decision (D3): PLean owns its proof tactic.** For Phase 1's hand-written M1
we discharge triples with the *raw* Loom primitives — `wpgen` to reduce the
triple to a VC, then `loom_solver`-equivalent (`grind` by default; the
`loom.solver` option in `Loom/MonadAlgebras/WP/Options.lean` defaults to `grind`,
which needs **no external solver binary**). PLean's own `loom_solve`-equivalent
tactic — a thin wrapper mirroring the `loom_solve` elaborator in
`CaseStudies/Tactic.lean`, built from the Loom-lib primitives above — lands in
`Verify/Tactic.lean` in **Phase 3**, where it was
always scheduled. The fallback option (add `CaseStudies` as a dependency target
and `import CaseStudies.Tactic` wholesale) is rejected for v1: that code pulls
in ProofWidgets panels and is example-grade, not a stable API.

> **Corollary — M1 may need no SMT solver at all.** Because the default
> `loom_solver` is `grind`, the trivial ping-pong VCs likely close without
> z3/cvc5. We still confirm the SMT round-trip works (Risk R3) so Phase 3 can
> rely on it, but it is not on M1's critical path.

---

## The semantic core, grounded in the references

PVerifier's UCLID5 backend
([`Uclid5CodeGenerator.cs`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs))
defines the exact shapes Phase 1 must mirror. Verified line references below.

### GlobalState (mirror [`Uclid5CodeGenerator.cs:594-606`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L594-L606))

UCLID5 `P_StateAdt` is a record of `sent : [Label]boolean`,
`received : [Label]boolean`, `machines : [MachineRef]MachineStateAdt`, plus a
top-level `P_ActionCount : integer`
([`:1119`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L1119)).
The Lean encoding folds `actionCount` into the record:

```lean
structure [GlobalState](../PLean/Semantics/GlobalState.lean#L57-L68) where
  sent        : Label → Bool          -- mirrors [Label]boolean
  received    : Label → Bool
  machines    : MachineRef → MachineState
  actionCount : Nat
```

- **`sent`/`received` as `Label → Bool`** matches PVerifier's `[Label]boolean`
  exactly and stays in SMT array/function territory (D4). (`Set Label` is an
  alternative; `Label → Bool` is closer to the UCLID5 encoding and to what
  `loom_smt`/cvc5 handles natively.)
- **`MachineState`** mirrors `MachineStateAdt = { stage : Bool, machine : MachineAdt }`
  ([`:645-649`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L645-L649)):
  `stage` is the entry flag, `machine` carries the current state-enum + fields.

### Label / EventOrGoto (mirror [`:757-766`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L757-L766))

```lean
structure [Label](../PLean/Semantics/Label.lean#L68-L72) where
  target      : MachineRef
  action      : EventOrGoto
  actionCount : Nat            -- the global counter at creation time
```

`actionCount`-on-Label is the witness the temporal `≺` operator reduces to
(`a ≺ b := a.actionCount < b.actionCount`) — see PLAN.md "Open Design
Problems." No `GlobalState` shape change is needed for `≺`; the field already
exists, so Phase 1 just defines the `def` and Phase 2 adds notation.

### Primitives (mirror [`:1967-1999`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L1967-L1999))

| Primitive | State update (verbatim from PVerifier) |
|---|---|
| `send tgt ev payload` | add `Label{tgt, Event ev payload, actionCount}` to `sent`; `actionCount += 1`; (Phase 4: notify spec observers) |
| `goto st payload` | add `Label{this, Goto st payload, actionCount}` to `sent`; `actionCount += 1`; set machine's state := `st`, `stage := true` |
| `raise ev payload` | like `send` to `this` (intra-machine) |
| `announce ev payload` | broadcast variant of `send` (Phase 4 spec hook) |
| `newMachine kind args` | allocate a fresh `MachineRef`, register initial `MachineState` |

### Default invariants (mirror [`:1189-1201`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L1189-L1201))

```lean
def [UniqueActions](../PLean/Semantics/Default.lean#L23-L26)    (s : GlobalState) : Prop :=
  ∀ a b, a ≠ b → s.sent a → s.sent b → a.actionCount ≠ b.actionCount
def [IncreasingCount](../PLean/Semantics/Default.lean#L36-L37)  (s : GlobalState) : Prop :=
  ∀ a, s.sent a → a.actionCount < s.actionCount
def [ReceivedSubsetSent](../PLean/Semantics/Default.lean#L46-L47) (s : GlobalState) : Prop :=
  ∀ a, s.received a → s.sent a
```

These are the obligations every handler must preserve, conjoined with the user
invariants — exactly the `requires`/`ensures` PVerifier emits per handler.

### Per-handler triple shape (mirror [`:1432-1591`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L1432-L1591))

For a handler of event `ev` in state `S` of machine `M`, PVerifier's procedure
requires: `inflight lbl` (sent ∧ ¬received), `lbl targets M`, `stateOf M = S`,
`lbl is ev`, the three sanity invariants, and the user invariants; and ensures
the sanity + user invariants. In Lean (`l := GlobalState → Prop`):

```lean
triple
  (fun s => Inv s ∧ inflight lbl s ∧ lbl.target = this
            ∧ stateOf this s = S ∧ lbl is ev)
  (M.S.ev_handler this (payloadOf lbl))
  (fun _ => Inv)
```

Phase 1 hand-writes one concrete instance of this shape (M1). Phase 3 generates
it from the registry for every `(M, S, ev)`.

---

## Confirmed design decisions (Phase 1)

1. **D1 — Monad stack: [`PM := NonDetT (StateT GlobalState DivM)`](../PLean/Semantics/Monad.lean#L38-L39)** (NonDetT
   outermost). Deviates from PLAN.md's inner-NonDetT order; justified by
   Cashmere being the only surviving reference and by Loom's loop/lift
   machinery assuming NonDetT on top. Confirmed empirically in Task 1.
2. **D2 — Reference DSL is Cashmere, not Velvet.** Velvet is absent at our pin.
3. **D3 — PLean discharges triples with raw Loom primitives in Phase 1; builds
   its own `loom_solve`-equivalent in Phase 3.** `loom_solve` itself is
   CaseStudies-only and not depended upon.
4. **D4 — [`sent`/`received : Label → Bool`](../PLean/Semantics/GlobalState.lean#L57-L68)** (matches PVerifier `[Label]boolean`).
5. **D5 — v1 verification mode: partial-correctness + demonic choice.** Required
   to bring the `scoped` `MAlgOrdered` instances into scope; demonic = safety
   over all schedules. Baked into the semantics facade via `open` (+ the
   `set_option loom.semantics.*` equivalents documented inline).
6. **D6 — `≺` via `actionCount`** (unchanged from PLAN.md; the field already
   exists on [`Label`](../PLean/Semantics/Label.lean#L50-L54), so no state-shape change).
7. **D7 — Keep `Stub.PM := Id` and the macro path untouched in Phase 1.** M1 is
   hand-written with no macros (matching PLAN.md's "First Deliverable"). The
   real `PM` is introduced alongside the stub under `Semantics/`. Repointing the
   `Surface/Stmt.lean` macros from `Stub` onto the real primitives is **Phase 2**
   surface work (it also synthesizes the per-program `EventAdt`/`MachineAdt`
   unions that the real `send` needs). This keeps the PingPong demo and
   bootstrap tests green throughout Phase 1.
   - *Alternative considered:* replace `Stub.PM` now and repoint macros (Option
     B). Rejected for Phase 1 — more churn, and the union synthesis it forces is
     genuinely Phase-2 work. Revisit if M1 reveals the stub and real PM must
     converge sooner.

---

## Module list (Phase 1 deliverables)

New files under `Semantics/` and `Tests/Semantics/`; `Stub.lean` stays for the
macro path (D7). Layout follows PLAN.md's "Module Layout."

```
Src/PLean/PLean/
  Semantics/
    [Label.lean](../PLean/Semantics/Label.lean)         # Label, EventOrGoto, MachineState; payloadOf/actionCount accessors
    [GlobalState.lean](../PLean/Semantics/GlobalState.lean)   # GlobalState record (sent/received/machines/actionCount)
    [Monad.lean](../PLean/Semantics/Monad.lean)         # PM := NonDetT (StateT GlobalState DivM); PProp := GlobalState→Prop;
                        #   re-exports MAlgOrdered/MAlgLift; bakes `open PartialCorrectness
                        #   DemonicChoice` so triples elaborate without ceremony (D5)
    [Primitives.lean](../PLean/Semantics/Primitives.lean)    # send/raise/goto/announce/newMachine as PM combinators
    [Predicates.lean](../PLean/Semantics/Predicates.lean)    # inflight, sent, received, `is`, `targets`, stateOf, ≺
    [Default.lean](../PLean/Semantics/Default.lean)       # UniqueActions, IncreasingCount, ReceivedSubsetSent

  (Verify/Tactic.lean is Phase 3 — NOT built here; M1 uses raw wpgen+grind)

Src/PLean/Tests/Semantics/
  [StackSpike.lean](../Tests/Semantics/StackSpike.lean)      # Task 1: #synth the instances, prove a trivial triple
  [HandPingPong.lean](../Tests/Semantics/HandPingPong.lean)    # M1: hand-written 2-state ping-pong, 4 handler triples
  [Combinators.lean](../Tests/Semantics/Combinators.lean)     # PM unit tests via .run (cf. Cashmere's #eval ....run.run)
```

`Stub.lean` is **not** deleted in Phase 1. It is retired in Phase 2 when the
surface macros move onto the real `PM`.

---

## Phase 1 work breakdown (ordered)

Tasks are sequenced so the highest-risk, lowest-cost confirmation (the stack
spike) gates everything else.

1. **Stack spike — the de-risk gate** ([`Tests/Semantics/StackSpike.lean`](../Tests/Semantics/StackSpike.lean), ~½ day).
   Define `PM := NonDetT (StateT GlobalState DivM)` over a *throwaway* 2-field
   `GlobalState`. Then, after `open PartialCorrectness DemonicChoice`:
   - `#synth Monad PM`, `#synth LawfulMonad PM`,
     `#synth MAlgOrdered PM (GlobalState → Prop)` — confirm all resolve.
   - Prove `triple (fun _ => True) (pure ()) (fun _ _ => True)` and a
     `get`/`set`/stub-`send` triple via `wpgen; grind`.
   - **Also try the PLAN.md order** `StateT GlobalState (NonDetT DivM)` and
     record which synthesizes + proves. Lock D1 on the evidence.
   Exit: a green file + a one-line note in STATUS confirming the layer order.

2. **Label / EventOrGoto / MachineState** ([`Semantics/Label.lean`](../PLean/Semantics/Label.lean)). Mirror
   PVerifier 757-766 / 645-649. For Phase 1, `EventOrGoto` and the payload type
   may be *specialized to the ping-pong events* (concrete `inductive`), with a
   `/- Phase 2 generalizes to the per-program union -/` marker. Define
   `payloadOf`, `actionCount` accessors.

3. **GlobalState** ([`Semantics/GlobalState.lean`](../PLean/Semantics/GlobalState.lean)). The record from "GlobalState"
   above. Add `Inhabited`/`Nonempty` (NonDetT's CCPO machinery wants inhabited
   carriers).

4. **PM monad + facade** ([`Semantics/Monad.lean`](../PLean/Semantics/Monad.lean)). `abbrev PM`, `abbrev PProp :=
   GlobalState → Prop`, the `open PartialCorrectness DemonicChoice`, and any
   `export`/`attribute` needed so downstream files get the instances. This is
   the file that "replaces the stub" conceptually, though `Stub.lean` physically
   remains for the macro path (D7).

5. **Primitives** ([`Semantics/Primitives.lean`](../PLean/Semantics/Primitives.lean)). `send`/`goto`/`raise`/
   `announce`/`newMachine` per PVerifier 1967-1999, as `modify`-style `PM`
   combinators. Unit-test each with `.run` against a concrete start state
   (Cashmere shows the idiom: `#eval (prog args).run.run.run initState`).

6. **Predicates** ([`Semantics/Predicates.lean`](../PLean/Semantics/Predicates.lean)). `inflight`, `sent`, `received`,
   `lbl is ev`, `lbl targets m`, `stateOf m`, and `≺`. Names mirror P surface
   keywords ([`PLexer.g4:72`](../../PCompiler/CompilerCore/Parser/PLexer.g4#L72)),
   not C# AST names. `is`/`targets`/`≺` are pure `Label` predicates;
   `inflight`/`sent`/`stateOf` are `GlobalState → Prop`.

7. **Default invariants** ([`Semantics/Default.lean`](../PLean/Semantics/Default.lean)). The three from
   PVerifier 1189-1201, verbatim.

8. **M1 — hand-written ping-pong proof** ([`Tests/Semantics/HandPingPong.lean`](../Tests/Semantics/HandPingPong.lean)).
   A single 2-state machine (states `Idle`, `Active`) with four defs
   (`Idle_entry`, `Idle_ePing`, `Active_entry`, `Active_ePong`), the user
   invariant **"every `ePong` is in response to a prior `ePing`"** stated with
   `≺` (this showcases PLean's headline temporal feature, which PVerifier cannot
   express), and four `triple` lemmas of the shape in "Per-handler triple shape."
   Discharge each via `wpgen` then `grind` (the default `loom_solver`); escalate
   to `loom_smt [...]` only if `grind` stalls.

9. **Combinator + triple regression tests** ([`Tests/Semantics/Combinators.lean`](../Tests/Semantics/Combinators.lean)).
   `.run`-based unit tests for the primitives + the M1 triples kept as
   regressions. Wire into the `Tests` lean_lib so `lake build` exercises them.

10. **SMT round-trip confirmation** (Risk R3, can run parallel to 8-9). From a
    PLean file, force `set_option loom.solver "cvc5"` on one M1 VC and confirm
    `loom_smt` finds and runs the solver binary the lakefile downloads into
    `loomBuildDir`. If the `currentDirectory!` path resolution (the
    `currentDirectory!` term elaborator in `Loom/SMT.lean`) doesn't locate the
    binary from a PLean source dir, record the fix needed for Phase 3. Not on
    M1's critical path (grind default).

---

## Exit criterion (M1)

From [`PLAN.md` § "First Deliverable"](PLAN.md#first-deliverable-15-weeks-end-of-phase-1):

- [`Semantics/{Label,GlobalState,Monad,Primitives,Predicates,Default}.lean`](../PLean/Semantics) build
  with `lake build`.
- [`Tests/Semantics/HandPingPong.lean`](../Tests/Semantics/HandPingPong.lean) defines `GlobalState`, `PM`, the
  primitives, the three default invariants, the four handler defs, and the
  temporal user invariant — and **all four handler triples discharge** (via
  `wpgen` + `grind`, no `sorry`).
- [`Tests/Semantics/Combinators.lean`](../Tests/Semantics/Combinators.lean) `.run`-evaluates the primitives to the
  expected `GlobalState` deltas (sent grows, actionCount increments).
- The Phase-0 PingPong demo and `Tests/Bootstrap/*` **still build** (D7 — the
  stub macro path is untouched).
- STATUS.md updated: Phase 1 → ☑, M1 milestone checked, D1 (layer order) and the
  Velvet/`loom_solve` findings folded into the Decision Log.

No surface macros change. No obligation generation. Those are Phases 2-3.

---

## Risks / things to watch

Updated from PLAN.md "Risks" with Phase-0's findings folded in.

- **R1 — `MAlgOrdered` composition (was #1; now low).** Cashmere's identical
  stack shape proves the instances compose. Residual risk is only the layer
  order (D1) and the `scoped`-instance `open` (D5), both resolved in Task 1.
  *Downgraded from "spike before committing" to "confirm in the first ½ day."*
- **R2 — `loom_solve` not in the depended lib (NEW, medium).** Resolved by D3
  (use raw primitives in Phase 1; build PLean's own tactic in Phase 3). The
  watch item is that the raw `wpgen`/`loom_smt` API is Loom-internal and could
  shift if we ever bump the pin — pin discipline matters.
- **R3 — SMT binary round-trip from a PLean file (NEW, low).** The lakefile
  already downloads z3/cvc5 into `loomBuildDir` to satisfy Loom's
  `currentDirectory!` path resolution. Confirm it actually fires from PLean
  sources (Task 10). Mitigated by the `grind` default needing no binary.
- **R4 — heterogeneous event payloads / the `EventAdt`/`MachineAdt` union
  (NEW, medium).** PVerifier builds one tagged union per program for all
  events/machines. Phase 1 sidesteps this by *specializing* M1's `EventOrGoto`
  to the ping-pong events. The generic union is Phase-2 `#gen_module` work
  (synthesize the inductive from the registry). Flagged so Phase 1's `Label`
  design leaves room for it.
- **R5 — map/seq `[K]Option V` parity (unchanged).** PVerifier encodes P maps
  as `[K]Option V`
  ([`:2190-2192`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs#L2190-L2192))
  to stay in decidable theories. Ping-pong has no maps, so M1 doesn't exercise
  this — but match the encoding when machine `var`s of map type first appear
  (Phase 2/6) or proofs may stall in PLean that pass in PVerifier.
- **R6 — Stmt-macro repointing (NEW, low).** Deferred to Phase 2 by D7. The risk
  is only that the real `send` signature (typed payload, `MachineRef` target)
  differs from `Stub.send`'s polymorphic-target stub
  (`Stub.lean:29`); Phase 2 must reconcile the
  surface at that point. No Phase-1 impact.
- **R7 — `noncomputable` instances (NEW, low).** Loom's `MAlgOrdered`/`wp`
  instances for `NonDetT` are `noncomputable` (the scoped `MAlgOrdered (NonDetT m) l`
  instance in `Loom/MonadAlgebras/NonDetT/Basic.lean`). The *verification*
  artifacts (triples) are `noncomputable` Props — fine. The *executable* `.run`
  tests (Task 9) rely on the plain `Monad` instance, which is computable. Keep
  the two paths separate; don't mark the primitives `noncomputable`.

---

## Hand-off to Phase 2

By end of Phase 1:

- A real `PM` and its `MAlgOrdered`/`MAlgLift` instances are in scope, with the
  layer order and verification mode locked (D1, D5).
- `send`/`goto`/`raise`/`announce`/`newMachine`, the state predicates, `≺`, and
  the three default invariants exist as ordinary Lean defs.
- M1 proves the per-handler triple shape works end-to-end with Loom's automation
  on a real temporal safety property.
- The stub macro path is untouched and still green.

Phase 2 then: (a) repoints `Surface/Stmt.lean` macros from `Stub` onto the real
primitives, (b) teaches `#gen_module` to synthesize the per-program
`EventAdt`/`MachineAdt` unions (R4) and the real `GlobalState` wiring, (c) adds
the `≺` notation + input shortcut, and (d) re-expresses the Phase-1 hand-written
ping-pong in surface syntax — which must still verify (M2). The hand-written M1
file becomes the elaboration target the macros are validated against.

---

## References

**In-repo** (clickable):

- **PVerifier UCLID5 backend** (the semantics PLean mirrors):
  [`Uclid5CodeGenerator.cs`](../../PCompiler/CompilerCore/Backend/PVerifier/Uclid5CodeGenerator.cs)
  — GlobalState 594-606, Label 757-766, actionCount 768-792, primitives
  1967-1999, handler triples 1432-1591, default invariants 1189-1201, map
  encoding 2190-2192
- [`PLAN.md`](PLAN.md) — overall plan (note: its Phase-1 monad order and
  `loom_solve` exit criterion are superseded by D1 and D3 here)
- [`STATUS.md`](STATUS.md) — living tracker

**Loom dependency** (vendored under the build tree; public repo
`verse-lab/loom` at the pin in [`lakefile.lean`](../lakefile.lean) — cited by
module path + def name, not linkable from here):

- **Cashmere** (the surviving reference DSL — *replaces* PLAN.md's Velvet
  citations): `CaseStudies/Cashmere/Cashmere.lean`,
  `CaseStudies/Cashmere/Syntax_Cashmere.lean`
- **Monad-algebra core**: `Loom/MonadAlgebras/Defs.lean` (`MAlgOrdered`,
  `MAlgLift`, `MAlgLiftTTrans`), `Loom/MonadAlgebras/Instances/{StateT,ExceptT,Basic}.lean`,
  `Loom/MonadAlgebras/NonDetT/Basic.lean`
- **WP / tactic primitives** (what PLean's Phase-3 tactic is built from):
  `Loom/MonadAlgebras/WP/Basic.lean` (`triple`, `wp`, `spec`),
  `Loom/MonadAlgebras/WP/Gen.lean` (`wpgen`),
  `Loom/MonadAlgebras/WP/Tactic.lean` (`loom_intro`, `mwp`),
  `Loom/SMT.lean` (`loom_smt`),
  `Loom/MonadAlgebras/WP/Options.lean` (`loom.solver` defaults, the
  `scoped`-opening `set_option`s)
- **`loom_solve` reference implementation** (NOT depended on; the template for
  PLean's Phase-3 tactic): `CaseStudies/Tactic.lean`;
  `velvetObligations` scaffold in `CaseStudies/Extension.lean`
