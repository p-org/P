/-
PLean.Semantics.Label — runtime artefacts that flow through the buffer.

Mirrors PVerifier's UCLID5 encoding (the C# reference is the source of
truth):
- `Label`     ↔ `LabelAdt`         { target, action, actionCount }
- `EventOrGoto` ↔ `EventOrGotoAdt` (event payload | goto payload)
- `MachineState` ↔ `MachineStateAdt` { stage, machine }

Phase 1 keeps these *generic* in the user-program-specific union types
(`E`/`G`/`M`). The concrete tagged unions are synthesized per-program in
Phase 2's `#gen_module`. For Phase-1 hand-written examples the user
provides the unions directly (see `Tests/Semantics/HandPingPong.lean`).

Why generic? PVerifier emits one tagged union per program (`MachineAdt`,
`EventAdt`, `GotoAdt`). PLean's runtime artefacts are parameterised over
those unions so the semantic core compiles once, regardless of what the
program-specific unions look like.
-/

namespace PLean

/-- Reference to a machine instance. Phase 1 uses a single global name
space; per-machine refinements (e.g., `MachineRef Server`) can be added
in Phase 2 once the registry knows the union shape. -/
abbrev MachineRef : Type := Nat

/-- A label's action is either an event with its payload, or a goto with
its payload. Generic in `E` (the event-payload union) and `G` (the goto
payload union). PVerifier's `EventOrGotoAdt` ties these to the
program-specific `EventAdt` and `GotoAdt` ([Uclid5CodeGenerator.cs:801-806]). -/
inductive EventOrGoto (E : Type) (G : Type) where
  /-- A user event with its payload. -/
  | event (e : E)
  /-- A goto transition with its (possibly trivial) payload. -/
  | goto  (g : G)
  deriving Inhabited, DecidableEq, Repr

/-- A label is the buffer entry that machines deliver to each other.
Generic in the action's event/goto unions. The `actionCount` field is
the temporal witness for the `≺` precedence operator (defined in
`Predicates.lean`).

Mirrors PVerifier's record at `Uclid5CodeGenerator.cs:762-766`:
```
type Label = record { target : MachineRef, action : EventOrGoto,
                      actionCount : integer }
```
-/
structure Label (E : Type) (G : Type) where
  target      : MachineRef
  action      : EventOrGoto E G
  actionCount : Nat
  deriving Inhabited, DecidableEq, Repr

/-- One machine instance's runtime state. `stage` is PVerifier's entry
flag (`true` = entry handler is the next thing to run). `currentState`
is the program-state-tag enum (provided by the user program); `fields`
holds the machine's `var` block, also user-provided.

Mirrors `MachineStateAdt` at `Uclid5CodeGenerator.cs:614-619`. -/
structure MachineState (S : Type) (F : Type) where
  /-- Entry flag: is the entry handler the next thing to run? -/
  stage        : Bool
  /-- Discrete control state of the machine. -/
  currentState : S
  /-- Machine's local state (its `var` block), as a record. -/
  fields       : F
  deriving Inhabited, Repr

/-! ## Accessors mirroring PVerifier's UCLID5 helpers -/

namespace Label

variable {E G : Type}

/-- True iff the label's action is an event tag. -/
@[inline] def isEvent (lbl : Label E G) : Bool :=
  match lbl.action with
  | .event _ => true
  | .goto  _ => false

/-- True iff the label's action is a goto tag. -/
@[inline] def isGoto (lbl : Label E G) : Bool :=
  match lbl.action with
  | .event _ => false
  | .goto  _ => true

/-- Project the event payload, if this is an event label. -/
@[inline] def eventPayload? (lbl : Label E G) : Option E :=
  match lbl.action with
  | .event e => some e
  | .goto  _ => none

/-- Project the goto payload, if this is a goto label. -/
@[inline] def gotoPayload? (lbl : Label E G) : Option G :=
  match lbl.action with
  | .event _ => none
  | .goto  g => some g

end Label

end PLean
