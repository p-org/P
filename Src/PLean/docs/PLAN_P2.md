# PLean — Phase 2 (Registry + minimal surface) Plan

This document expands the Phase-2 entry in [`PLAN.md`](PLAN.md). The
objective of Phase 2 is to make Phase 1's real `PM` reachable from the
Phase-0 surface — to **bridge the macro path onto the real semantic
core** so a user-written `pmodule` that compiles with `#gen_module`
*also* produces `triple` lemmas of the M1 shape that can be discharged
with `wpgen` + raw Loom primitives.

Phase 2 ends with **M2**: the four-file PingPong demo from Phase 0,
re-expressed only in surface syntax, *verifies* — i.e., its handler
triples discharge automatically the same way M1's did.

> **Read this first.** Phase 1 left two distinct paths through PLean:
> the Phase-0 macro path elaborates onto `PLean.Stub.PM := Id` (no-op
> primitives, no verification); the Phase-1 hand-written path uses the
> real `PM := NonDetT (StateT _ DivM)`. Phase 2 collapses these into
> one. The `Stub.lean` module is **retired** at the end of Phase 2.

---

## What Phase 1 left in place

### The real semantic core (used)
- `PLean.Semantics.{Label, GlobalState, Monad, Primitives, Predicates,
  Default}` — fully built, parameterised over `ProgramSig`.
- `Tests/Semantics/HandPingPong.lean` — M1, the elaboration target
  Phase 2 must reproduce from surface syntax.

### The Phase-0 macro path (still elaborating onto stubs)
- `Surface/Module.lean` — `pmodule M ... end M` works (cross-file
  aggregation, registry).
- `Surface/Types.lean` — `type N` (foreign), `type N = …` (alias),
  `type N = (f : T, …)` (named-tuple struct), `enum N { … }`. Already
  defers materialisation to `#gen_module`.
- `Surface/Events.lean` — `event ev : T` registers metadata; at
  `#gen_module`-time emits `def ev : EventTag := <hash>` and
  `abbrev ev_payload : Type := T`. **`EventTag := Nat`** today —
  Phase 2 must replace this with a typed event-union projection.
- `Surface/Machine.lean` — `machine M { var x : T; state S { … } }`.
  Body `Syntax` saved verbatim; `#gen_module` replays it inside the
  pmodule namespace, opens `namespace M`, and emits handler defs of
  shape `def Idle.ePing_handler (this : Stub.MachineRef) (req : T) :
  Stub.PM Unit := do …`.
- `Surface/Stmt.lean` — statement macros emit
  `PLean.Stub.{send, raise, goto, new, announce}` calls. **Every one
  of these targets the stub.**
- `Surface/Verify.lean` — `invariant`, `paxiom`, `init-holds`,
  `function`, `pinstance`. All defer to `#gen_module`.
- `Commands/{PWf, PVerify, PrintModule}.lean` — structural validation;
  no obligation generation yet.

### The gap (concretely)
Run [`#print_pmodule`](../PLean/Commands/PrintModule.lean) on the
PingPong demo and you see `machine Server receives [ePing] sends
[ePong]` — the registry knows the structure. But the *generated Lean
code* is:

```lean
namespace PingPong.Server
abbrev Client : Type := PLean.Stub.MachineRef            -- ← stub ref
variable (client : Client)
def Idle.entry (this : PLean.Stub.MachineRef)
    (input : PingPong.ServerInit) : PLean.Stub.PM Unit :=  -- ← stub PM
  do
    let _ := client; let _ := input.client; pure ()         -- ← no-op assign
def Idle.ePing_handler (this : PLean.Stub.MachineRef) ...
    : PLean.Stub.PM Unit :=
  do PLean.Stub.send client ePong ...                        -- ← no-op send
end PingPong.Server
```

Phase 2 makes that emission *match* the Phase-1 M1 shape, where
- `Stub.MachineRef` → `PLean.MachineRef`,
- `Stub.PM` → `PLean.PM PingPong.Sig`,
- `Stub.send` → `PLean.send (P := PingPong.Sig)`,
- the no-op assignment becomes a real state update,
- `PingPong.Sig` is *synthesised* from the registry (not user-written).

---

## Confirmed design decisions (Phase 2)

These extend [`PLAN_P1.md` § "Confirmed design decisions"](PLAN_P1.md).
Numbering continues so `D8` here is `D8` in any cross-reference.

1. **D8 — One `Sig` per `pmodule`, synthesised by `#gen_module`.** The
   `ProgramSig` bundle (`E`/`G`/`S`/`F`) is built per-pmodule from the
   registry: `E` is the union of all event payload types; `G` is the
   union of all `goto` payload types (often `Unit`); `S` is the union
   of all states (across all machines, as in PVerifier's
   `MachineAdt_*_StateAdt`); `F` is the disjoint union of per-machine
   `Fields` records.

   The synthesised `Sig` lives at `<PMod>.Sig` (e.g.,
   `PingPong.Sig`). Handler defs reference it as `PM Sig` etc.

2. **D9 — Event payload abbrev: `<ev>_payload`** (unchanged from
   Phase 0). The `send` macro continues to ascribe its named-tuple
   literal to this abbrev.

3. **D10 — Machine names become *distinct* types, each a single-field
   wrapper around `MachineRef`.** *Revised from the initial draft of
   this plan; the original "abbrev `<MachineName> := MachineRef`"
   loses the type distinction we need for spec-side quantification.*

   Specifications that come in Phase 3 (and the temporal user
   invariants we already write today) will want to quantify over
   *just* the refs of one machine kind:

   ```p
   -- "every server eventually receives a client's ping"
   ∀ s : Server, ∃ p, sent p ∧ p targets s ∧ p is ePing
   ```

   With `abbrev Server := MachineRef`, the bound variable `s` ranges
   over *every* machine ref in the system — Server, Client, future
   spec machines, every user-defined kind. The invariant doesn't say
   what the user means.

   The fix: emit a single-field wrapper per machine, plus a coercion
   to `MachineRef` so existing primitives (`send`, `goto`, …) still
   accept it transparently:

   ```lean
   -- emitted by `#gen_module` for each machine `Server { ... }`:
   structure Server where
     ref : MachineRef
     deriving DecidableEq, Inhabited

   instance : Coe Server MachineRef := ⟨Server.ref⟩
   ```

   Each machine kind is now a *definitionally distinct* Lean type
   (so Lean's elaborator rejects `var c : Client` initialised with a
   `Server`-typed value), but transparently usable wherever
   `MachineRef` is expected (so `send server, ev, …` continues to
   compile when `server : Server`).

   Phase 3's obligation generator can then synthesise quantification
   restricted to the right machine kind — `∀ s : Server, …` binds
   exactly the server refs.

   *Veil's analogue:* `type node` declares a fresh sort + `Nonempty`
   instance and invariants quantify `∀ L1 L2 : node, …`. PLean's
   wrapper-struct version is the same idea with a concrete carrier
   (so we don't need `axiom`s and can `#eval`-test).

   *Alternative considered:* `opaque Server : NonemptyType` + an
   axiomatic `Server.toRef : Server → MachineRef`. Mirrors the
   existing PLean foreign-sort pattern but introduces `axiom`s for
   no benefit over the structure form. Rejected.

   *Tracked risk:* `R13 — Coe semantics across primitives` (see
   below). Lean's coercion may not fire through every macro form;
   we may need explicit `(server.ref)` projections in some emission
   sites.

4. **D11 — Handler defs go from `Stub.PM` to `PM <Mod>.Sig`, with a
   *typed* `this`.** That is the *single* repointing that makes the
   surface verifiable. M1's `(this : MachineRef)` convention shifts
   slightly per D10: the handler for state `S` of machine `M` takes
   `(this : M)` (the wrapper struct), not `(this : MachineRef)`. The
   `Coe M MachineRef` instance from D10 means handler-body call sites
   that pass `this` to a primitive (`send this, …`) keep compiling
   without change. The handler-name scheme
   `<machine>.<state>.<event>_handler` is unchanged.

   Concretely, M1's

   ```lean
   def Server.Idle.ePing_handler
       (this : MachineRef) (replyTo : MachineRef) (lbl : Lbl) : M' Unit
   ```

   becomes (under Phase 2 surface emission)

   ```lean
   def Server.Idle.ePing_handler
       (this : Server) (replyTo : MachineRef) (lbl : Lbl) : M' Unit
   ```

   The `replyTo` parameter type comes from the event payload — it
   stays `MachineRef` for now (Phase 2 doesn't try to type the
   `replyTo` field of `ePing`'s payload as `Client`; that's a
   refinement-of-payload-types extension, post-v1).

5. **D12 — `var x : T` becomes a real machine field, not a Lean
   section variable.** Phase 0 emits `variable (x : T)` so handler
   bodies that reference the var auto-bind it — but the variable is
   never *written* (the assignment macro is a no-op). Phase 2
   replaces this:
   - `var x : T` registers a field of type `T` in
     `<MachineName>.Fields`;
   - `<MachineName>.Fields` is a `structure` synthesised by
     `#gen_module`, contributing to `Sig.F`;
   - `var`-reads compile to `(machineState this.ref).fields.x`
     (the `.ref` projection follows from `this`'s typed wrapper, D10);
   - `var`-assignments compile to a `modify`-style state update on
     `machines this.ref`.

   When the field is itself a *machine* (`var server : Server`), the
   type stored in `Fields` is the wrapper struct. Storing typed
   refs is the whole point of D10: a future spec like `∀ c : Client,
   c.server = (some-fixed-Server-ref)` becomes statable.

   This is the substantive change of Phase 2 — it's what makes a
   handler body actually *do* something.

6. **D13 — `goto` updates `currentState` *and* enqueues a goto
   label**, mirroring `Primitives.goto`. Phase-0 `goto S` was a
   no-op; Phase-2 `goto S` becomes a real state machine transition.

7. **D14 — `#gen_module` emits `#derive_lifted_wp` for the
   per-program `get`/`set`.** The hand-written M1 has these on
   lines 79–85 of `HandPingPong.lean`; Phase 2 emits them
   automatically once `<Mod>.Sig` exists. Required so `wpgen` can
   step through handler bodies.

8. **D15 — `Stub.lean` is retired at the end of Phase 2.** The
   facade `import` in `PLean.lean` removes `PLean.Internal.Stub`;
   the file itself is deleted. Anything that still references
   `PLean.Stub.*` after Phase 2 either lives in retired tests or
   is a bug. Tracked in the regression checklist below.

9. **D16 — `≺` notation lands here, not in Phase 3.** PLAN.md and
   PLAN_P1 both flagged `notation:50 a " ≺ " b => …` as Phase 2
   work. This is the smallest piece of new surface and the headline
   PLean feature; it goes in `Surface/Notation.lean`. Adding it now
   means the Phase-2 PingPong example can state the temporal
   invariant in P-style syntax.

10. **D17 — `#pverify M` still delegates to `#pwf M` in Phase 2.**
    Obligation generation is Phase 3. After Phase 2, `#pverify` is
    "well-formed" + "every handler def has the right shape and
    type-checks against the real PM" — checking the *macro path
    machinery*, not Hoare triples. That last bit goes in `#pverify`
    via a small "ensure every machine's handler defs exist as
    Lean constants" step.

---

## The semantic-core surface area Phase 2 needs

PLean's Phase-1 deliverables (read-only as far as Phase 2 is
concerned):

| Need from Phase 1 | Where it lives |
|---|---|
| `PM`, `PProp`, `ProgramSig` | `Semantics/Monad.lean` |
| `Label`, `EventOrGoto`, `MachineState` | `Semantics/Label.lean` |
| `GlobalState`, update helpers | `Semantics/GlobalState.lean` |
| `send`/`goto`/`raise`/`announce`/`markReceived` | `Semantics/Primitives.lean` |
| `inflight`/`sent`/`isEvent?`/`targets?`/`stateOf`/`precedes` | `Semantics/Predicates.lean` |
| `DefaultInvariants` | `Semantics/Default.lean` |

Everything Phase 1 promised. Phase 2 imports these, doesn't modify
them.

---

## Module list (Phase 2 deliverables)

```
Src/PLean/PLean/
  Surface/
    Notation.lean          # NEW: `≺`, `is`, `targets`, `inflight`, `sent` notations
    Stmt.lean              # MODIFIED: macros target real PM (D11/D12)
    Machine.lean           # MODIFIED: emit machine state record + handler defs
    Events.lean            # MODIFIED: emit event union contributors
    (Module/Types/Verify.lean — unchanged from Phase 0)

  Commands/
    GenModule.lean         # NEW (was nested in Machine.lean): the heavy
                           #   lifter — synthesises Sig from registry, emits
                           #   the union types, emits #derive_lifted_wp,
                           #   emits handler defs over real PM
    PVerify.lean           # MODIFIED: add "handler defs exist + type-check"
                           #   structural check (D17)
    (PWf/PrintModule.lean — unchanged)

  Internal/
    Stub.lean              # DELETED at end of Phase 2 (D15)

Src/PLean/Tests/Surface/
  Phase2PingPong.lean      # NEW: M2 — surface-syntax ping-pong with the
                           #   four handler triples discharged via wpgen +
                           #   raw Loom primitives, mirroring HandPingPong.lean
                           #   shape exactly
  Combinators.lean         # NEW: `.run`-based regression that the surface-
                           #   emitted handlers compute the right buffer
                           #   deltas (mirrors Tests/Semantics/Combinators)
```

---

## Phase 2 work breakdown (ordered)

The order minimises rework: each step preserves a green build, with
the stub-vs-real bridge introduced incrementally.

### 1. **`≺` notation + accessor desugaring** — small, isolated, low risk
*(`Surface/Notation.lean`, ~½ day)*

Add `notation:50 a " ≺ " b => PLean.precedes a b` and the
`a is e` / `a targets m` / `inflight a` notations as front-end sugar
for the predicates already defined in `Semantics/Predicates.lean`.
No registry changes. Confirms the predicates' API works through
Lean's notation system before we depend on it in M2.

Exit: a tiny test in `Tests/Surface/Notation.lean` that uses each
notation in a Lean `Prop` and checks it elaborates.

### 2. **`Sig` synthesis from registry** — the load-bearing step
*(new `Commands/GenModule.lean` extracted from `Surface/Machine.lean`,
~1 day)*

`#gen_module M` becomes the place where the per-program union types
are built. After all `event`/`type`/`enum`/`machine` decls are
collected, emit (in order):

1. **Per-machine wrapper types (D10).** For each machine declared
   in the module, emit:

   ```lean
   structure <MachineName> where ref : MachineRef
     deriving DecidableEq, Inhabited
   instance : Coe <MachineName> MachineRef := ⟨<MachineName>.ref⟩
   ```

   These come *first* because subsequent steps reference them: the
   `Fields` records may store machine wrappers as field types; the
   handler defs use the wrapper as `this`'s type.

2. `<Mod>.E` — an `inductive` whose ctors are the events. Each
   ctor's payload type is the event's `payloadTy` (single `Unit`-
   carrying ctor for events without payload).
3. `<Mod>.S` — `inductive` over states across all machines (PVerifier
   pattern). Ctor names are `<MachineName>_<StateName>` to avoid
   collision when two machines share state names.
4. `<Mod>.Fields` — a structure with one field per `var x : T`,
   prefixed by machine name (e.g. `Server_client`). When `T` is
   another machine name, the field type is the wrapper struct from
   step 1, so machine-typed fields carry their kind.
5. `<Mod>.G := Unit` (Phase 2 leaves goto payloads trivial; the
   surface doesn't expose a way to declare goto-payload types yet
   anyway).
6. `abbrev <Mod>.Sig : ProgramSig := { E := E, G := G, S := S, F := Fields }`.
7. `abbrev <Mod>.PM' (α : Type) := PM Sig α` — the per-program
   monad alias.
8. `abbrev <Mod>.GS := GlobalState Sig`.

These mirror the Phase-1 hand-written ones in `HandPingPong.lean`
lines 30–61 verbatim, just under a synthesised name.

> **Implementation note:** `#gen_module` is currently inside
> `Surface/Machine.lean`. Extract to its own file before adding this
> logic — it'll be too large to keep nested.

Exit: `#gen_module PingPong` followed by `#check @PingPong.Sig` and
`#check @PingPong.PM' Unit` resolve.

### 3. **`#derive_lifted_wp` emission for `get`/`set`** — small follow-up
*(`Commands/GenModule.lean`, ~½ day)*

After step 2, `<Mod>.GS` exists. Append two
`#derive_lifted_wp` calls per pmodule:

```lean
#derive_lifted_wp for
  (get : StateT <Mod>.GS DivM <Mod>.GS)
  as <Mod>.PM' <Mod>.GS

#derive_lifted_wp (s : <Mod>.GS) for
  (set s : StateT <Mod>.GS DivM PUnit)
  as <Mod>.PM' PUnit
```

These are the lemmas `wpgen` uses to step through the inner-StateT
operations. Without them, surface-emitted handler triples stall the
same way M1 did before Cashmere's `#derive_lifted_wp` calls were
added.

Exit: M1's `#derive_lifted_wp` lines can be removed and M1 still
compiles, when M1 is reframed against the synthesised `PingPong.PM'`
(test by hand).

### 4. **Repoint `Surface/Stmt.lean` macros** — the bridge
*(~1 day)*

Each `Stub.X` reference becomes `PLean.X` parameterised by `Sig`.
Concretely:

| Old emission | New emission |
|---|---|
| `Stub.send target ev payload` | `send (P := <Mod>.Sig) (target : MachineRef) ev` (payload is a *named* `Sig.E` ctor application; `target` is coerced to `MachineRef` via D10's `Coe`) |
| `Stub.raise ev payload` | `raise (P := <Mod>.Sig) this.ref ev` |
| `Stub.goto stTag payload` | `goto (P := <Mod>.Sig) this.ref <state-ctor> .unit` |
| `Stub.new tag args` | `newMachine (P := <Mod>.Sig) this.ref <tag>` |
| `Stub.announce ev p` | `announce (P := <Mod>.Sig) this.ref ev` |
| `let _ := lhs; let _ := rhs; pure ()` (assignment) | A real `modify`/state update via `<Mod>.GS.fields.<machine>_<field>` |

The `<Mod>.Sig` is in scope because the handler def is elaborated
inside the pmodule namespace and `#gen_module` already emitted `Sig`
in step 2.

The named-tuple payload macro changes shape: instead of building
`{ id := …, status := … } : ePong_payload`, it now builds the full
event ctor `Sig.E.ePong { id := …, status := … }` (or
`Sig.E.ePong (.mk … …)` if the payload is positional). The
discriminator is the event ident the user wrote.

Exit: `lake build Examples` and `lake build Tests` still succeed,
and `#print_pmodule PingPong` from `Top.lean` still reports the same
shape. Handler defs now have type `PM PingPong.Sig Unit`, not
`Stub.PM Unit` — verify with `#check`.

### 5. **`var`-reads and assignments become real state updates** — D12
*(`Surface/Machine.lean` materialisation + `Surface/Stmt.lean`
assignment macro, ~1 day)*

This is where the Phase-0 stubs were truly empty. Two parts:

**5a. Reads.** A reference to `client` inside a handler body
(currently auto-included as `(client : Client)` via Lean
`variable`) compiles to:

```lean
(← (get : StateT <Mod>.GS DivM <Mod>.GS)).machines this.ref
  |>.fields.<MachineName>_client
```

Lean's `do`-notation lift handles the `←`. The chain projects out
the per-machine `Fields` slice and then the named field. The
`this.ref` projection unwraps the `<MachineName>` wrapper struct
(D10) — `s.machines` is keyed by `MachineRef`, not by the typed
wrapper.

**5b. Assignments.** `client = expr` becomes:

```lean
do
  let s ← (get : ...)
  let curr := s.machines this.ref
  let newFields := { curr.fields with <MachineName>_client := expr }
  let newMachineState := { curr with fields := newFields }
  set (s.updateMachine this.ref newMachineState)
```

This is verbose enough to warrant a `_root_.PLean.writeField` helper
that takes `(this : MachineRef) (proj : F → α) (v : α) : PM Unit`.
Let's call it that.

The trickiest piece is *which machine's `Fields` slice*. The
emitter knows from context (the def is inside `namespace
<Mod>.<MachineName>`); use that to compute the field-projection
name (`<MachineName>_client`).

Exit: a hand-test where `Server.Idle.entry`'s body
`client = input.client` actually mutates the global state — confirm
with a `.run`-style trace and a triple proof.

### 6. **`goto S` → real transition** — D13
*(`Surface/Stmt.lean`, ~½ day)*

The current emission is `Stub.goto (Name.hash str) ()`. New
emission:

```lean
goto (P := <Mod>.Sig) this.ref <Mod>.S.<MachineName>_<StateName> GotoP.unit
```

(`this.ref` because `Primitives.goto` is keyed on `MachineRef`, not
the typed wrapper. Coercion would also work but the explicit `.ref`
keeps the emitted code readable.)

The state ctor lookup is mechanical — `Sig.S` is a single inductive
with ctor `<MachineName>_<StateName>` per (machine, state) pair, and
the emitter knows both names from the lexical context.

Exit: a state transition compiles, and `goto` updates
`currentState`/`stage` per `Primitives.goto`.

### 7. **`Stub.lean` retirement** — D15
*(~½ day)*

After steps 4–6, no surface emission targets `Stub.*`. Confirm with:

```bash
grep -rn 'PLean\.Stub' Src/PLean/PLean Src/PLean/Examples Src/PLean/Tests
```

— should return 0 hits in non-deleted files. Then:

1. Delete `PLean/Internal/Stub.lean`.
2. Remove the `import PLean.Internal.Stub` from `PLean.lean`.
3. Run `lake build` — must stay green.

This is the "we don't need stubs anymore" milestone.

### 8. **Phase-0 PingPong demo verifies — M2** — the exit milestone
*(`Tests/Surface/Phase2PingPong.lean`, ~1-2 days)*

Take the existing `Examples/PingPong/{Events,Server,Client,Top}.lean`
files (or copies under `Tests/Surface/`), add the temporal user
invariant via `≺`, then *prove* the four handler triples by:

```lean
theorem Server_Idle_ePing_correct
    (this replyTo : MachineRef) (lbl : PingPong.Sig.Label) :
    triple (l := PingPong.PProp) <pre> <handler-def> <post> := by
  unfold <handler-def> <inv-defs>
  wpgen <;> first | apply WPGen.default | skip
  -- exact the same proof tail as M1's ePing_handler_correct
```

The proofs *must* be virtually identical to M1's — that's the test
that the surface-emitted handler defs match the M1 shape modulo
naming. If they're not identical, something in the surface-emission
diverged from the hand-written form and needs reconciliation.

The richer Phase-0 demo (with named-tuple payloads `(id = …, status =
…)`, multi-state `Client { Booting; Done }`, machine-var refs
`var client : Client`) exercises more of the surface than M1's
two-state-zero-var single-payload setup. **This is what makes M2
genuinely harder than M1** — and what justifies Phase 2's existence.

Exit: M2 ☑ in STATUS.md.

### 9. **Combinator regression for the surface** — non-blocker, runs
parallel to 8 *(~½ day)*

`Tests/Surface/Combinators.lean` mirrors the Phase-1
`Tests/Semantics/Combinators.lean` but exercises *surface-emitted*
handler defs. Confirms the surface's `var x = expr` actually mutates
state by `.run`-evaluating it. The Phase-1 file became regression for
the primitives; this becomes regression for the surface.

### 10. **Update `#pverify`'s structural check** — D17
*(~½ day)*

Add: for every registered `(machine, state, event)` in `M`, the
synthesised handler-def constant exists and type-checks at
`PM Sig α`. This catches "the macro generated something wrong"
errors that aren't well-formedness errors. Implement as a post-pass
on `#pverify M` — walk the registry, for each `(M, S, E)` `mkConst`
the expected def name and `mkAppN` it; if the expr fails to
elaborate, surface a clear error.

Exit: a deliberately broken handler (e.g., `send` with a wrong
event-payload type) is caught by `#pverify` *before* the user
reaches Phase 3's obligation generator.

---

## Exit criterion (M2)

From [`PLAN.md` § Phase 2](PLAN.md#phase-2--registry--minimal-surface-1-week):

> Rewrite the Phase-1 ping-pong example in surface syntax; still
> verifies.

Concretely, after Phase 2:

- `Tests/Surface/Phase2PingPong.lean` builds clean, with four `theorem
  …_correct` lemmas, no `sorry`, discharged via `wpgen` + the same
  manual tails M1 uses.
- `Examples/PingPong/{Events,Server,Client,Top}.lean` (the original
  Phase-0 demo, with the temporal invariant added) still produces a
  clean `#pwf` / `#pverify` and the synthesised handler defs type-
  check at the real `PM`.
- `PLean.Internal.Stub` is *deleted*; nothing imports it.
- `#print_pmodule PingPong` shows the same registry shape as Phase 0
  (no regression).
- `Tests/Bootstrap/*` and `Tests/Semantics/*` still build (Phase-0
  and Phase-1 regressions).
- A small standalone test confirms machine types are *distinct*
  (`PingPong.Server` and `PingPong.Client` are not the same type;
  `#check (fun (s : Server) => (s : Client))` fails to elaborate)
  and quantifiable (`∀ s : Server, True` parses and elaborates).
  This is the D10 acceptance test.
- STATUS.md: Phase 2 → ☑, M2 ☑, decision log entries for D8–D17
  added.

Phase 3 then layers on top: synthesises the *triples themselves*
from the registry (no hand-written `theorem … := by …` per handler),
and ships PLean's own `pverify` tactic that wraps `wpgen` + a
configurable solver chain.

---

## Risks / things to watch

Inherits PLAN_P1's residual list. New risks specific to Phase 2:

- **R8 — Synthesised inductive ordering.** `<Mod>.E` and `<Mod>.S`
  must be emitted *before* any handler def references their ctors.
  The Phase-0 registration-order arrays (`typeOrder` / `eventOrder`
  / `machineOrder`) already exist, but Phase-2's emission order is
  more rigid: types → events (defining `<Mod>.E`) → machine state
  records (defining `<Mod>.S` and `<Mod>.Fields`) → `Sig` →
  `PM'`/`GS` aliases → `#derive_lifted_wp` → handler defs. Get the
  order wrong and the synthesised inductive is unfindable when a
  handler tries to reference its ctor. *Mitigation:* enforce the
  order in `Commands/GenModule.lean` with a clearly-numbered
  emission pipeline and a comment matching this list.

- **R9 — Cross-file `Sig` consistency.** A `pmodule M` may span
  many files. Each file *registers* fragments; only `#gen_module M`
  *emits*. The tricky case: the user `import`s the `#gen_module`
  output into another file and expects `M.E`/`M.Sig` etc. to be
  visible. Our persistent env extension should already carry the
  registry across `import`; the synthesised Lean defs propagate
  through Lean's normal `import` mechanism. *Mitigation:* a
  multi-file regression test under `Tests/Surface/MultiFile/` that
  splits Phase-2 PingPong across files and confirms `import` of the
  `#gen_module`-bearing file makes the synthesised `Sig` visible.

- **R10 — Named-tuple payload positionality.** Phase 0's macro
  builds anonymous `{ f := v, … }` and ascribes to
  `ev_payload`. Phase 2's macro must build the *event ctor*:
  `Sig.E.ePing (.mk replyTo)` for a `replyTo`-payload event,
  `Sig.E.ePing` (no args) for a payload-less event. The mapping
  from "user wrote `(replyTo = c)`" to "Sig.E.ePing's argument" is
  the source of subtle bugs. *Mitigation:* per-event metadata
  records the payload arity (already tracked) and the macro reads
  it to choose between zero-arg ctor / named-record ctor /
  positional ctor. Test each case in
  `Tests/Surface/Phase2PingPong.lean`.

- **R11 — Multiple machines sharing a state name.** PVerifier's
  `MachineAdt_*_StateAdt` is per-machine; PLean's `<Mod>.S` is
  cross-machine. Two machines named `Idle` collide unless we
  prefix with the machine name. The `#pwf` validator already
  rejects within-machine duplicates; cross-machine duplicates
  *aren't* rejected today (and shouldn't be). *Mitigation:* the
  synthesised `<Mod>.S` ctor names use `<MachineName>_<StateName>`,
  and the `goto` macro prepends the lexical machine name. Test in
  `Tests/Surface/SharedStateName.lean`.

- **R12 — `Stub.PM := Id` retirement breaks reverse imports.**
  Some Phase-0 examples or tests might `import PLean.Internal.Stub`
  directly (rare but possible). Step 7 grep catches this; if any
  hits, fix them before deletion. *Mitigation:* the grep + the
  full Examples/Tests build during step 7.

- **R13 — `Coe <MachineName> MachineRef` doesn't fire through every
  emission site.** D10's typed-machine wrapper relies on Lean's
  coercion machinery to keep primitives (`send`, `goto`, …) calling
  with `MachineRef`. Coercions are tried at *application* sites, but
  some Phase-2 macro emissions construct intermediate terms (e.g.,
  the synthesised `Label.target` field of an event ctor's payload)
  where Lean may not insert the `Coe`. *Mitigation:* the emission
  table in step 4 explicitly threads `.ref` projections rather than
  trusting `Coe`. Reserve `Coe` for *user* code (where the user
  writes `send server, ev, …` and we want it to compile without
  requiring `.ref`); use explicit projections in macro-emitted
  source so we don't depend on coercion timing. Test in
  `Tests/Surface/MachineTyping.lean` — round-trip a handler body
  with both forms and confirm both elaborate.

- **R14 — Spec-side machine quantification (Phase 3 preview).** The
  whole point of D10's typed wrappers is that a Phase-3 invariant
  like `∀ s : Server, …` quantifies over the Server kind. But the
  *image* of `Server.ref : Server → MachineRef` is the set of refs
  the system has actually allocated as Servers — we have no machine
  fact saying so, just a runtime invariant the dispatcher would
  maintain. For Phase 2 this isn't a problem (we don't generate
  invariants yet); flag it for Phase 3 when the obligation
  generator must decide whether `∀ s : Server` ranges over (a) the
  whole `Server` type (every `Server.mk r` for any `r : MachineRef`),
  or (b) just refs that are actually present in `s.machines` with
  kind Server (the intended reading). The likely fix is a
  `validRef : Server → GlobalState Sig → Prop` predicate that
  obligation generation conjoins into the quantifier guard. **No
  Phase-2 work needed; just don't paint into a corner.**

---

## Hand-off to Phase 3

By end of Phase 2:

- One `pmodule M` produces a complete set of Lean defs, alongside a
  `<Mod>.Sig`/`<Mod>.PM'`/`<Mod>.GS` machinery, the
  `#derive_lifted_wp` lemmas, and handler defs that type-check at
  the real `PM`.
- Each machine name is a *distinct* Lean type (D10), so Phase 3 can
  generate quantifiers like `∀ s : Server, …` that bind only over
  the right machine kind. R14 flags the question of how to
  restrict that quantifier to *allocated* refs — Phase 3 settles it.
- M2 hand-writes the `theorem …_correct := by wpgen <;> ...` lemmas
  per handler.
- `#pverify` checks "well-formed + handler-defs type-check"; it
  does *not* yet check Hoare triples.

Phase 3 adds:

- `Verify/Obligation.lean` — walks the registry, for each `(M, S,
  E)` synthesises the per-handler triple lemma (one per handler,
  shape from M1 / M2).
- `Verify/Tactic.lean` — `pverify` tactic. Wraps `wpgen` + the
  configurable `loom_solve`-equivalent (a cut-down version of
  `CaseStudies/Tactic.lean`).
- `Verify/Sanity.lean` — auto-generates the default-invariant
  obligations (the `default` proof block from PVerifier
  `Uclid5CodeGenerator.cs:353-371`).
- `#pverify M` extends to *generate and discharge* the obligations,
  not just to validate well-formedness.

The test M3 marks "from one `pmodule` declaration, every handler
triple — both user-stated and default — is generated and proved
without the user writing a `theorem` line." That's the verification
flagship.

---

## References

**In-repo** (clickable):

- [`PLAN.md`](PLAN.md) — overall plan
- [`PLAN_P1.md`](PLAN_P1.md) — Phase 1 detailed plan (semantic core)
- [`STATUS.md`](STATUS.md) — living tracker
- [`HandPingPong.lean`](../Tests/Semantics/HandPingPong.lean) — the
  M1 elaboration target Phase 2 surface emissions must reproduce
- [`Examples/PingPong/`](../Examples/PingPong/) — Phase-0 demo;
  the M2 elaboration target after retemplating onto real PM
- [`Surface/Stmt.lean`](../PLean/Surface/Stmt.lean) — macros to
  repoint
- [`Surface/Machine.lean`](../PLean/Surface/Machine.lean) —
  `#gen_module` materialisation (extract to `Commands/GenModule.lean`)
- [`Internal/Stub.lean`](../PLean/Internal/Stub.lean) — to be
  deleted at end of Phase 2

**Loom dependency** (vendored under the build tree; cited by module
path + def name):

- `Loom.Meta.#derive_lifted_wp` — emits `loomSpec` lemmas for
  lifted state operations. Phase 2's `#gen_module` calls this once
  per pmodule.
- `Loom.MonadAlgebras.WP.Tactic.wpgen` — discharges triples after
  `apply WPGen.default` falls back. Used in M2 proofs.
- `CaseStudies/Cashmere/Cashmere.lean:31-33` — the `#derive_lifted_wp`
  emission pattern Phase 2 mirrors. *(Reference only; not
  imported.)*
