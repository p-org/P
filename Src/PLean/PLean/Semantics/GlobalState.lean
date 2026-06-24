/-
PLean.Semantics.GlobalState — the global system state.

The record has four fields:
- `sent`, `received` — characteristic functions on labels (`Label →
  Bool`). The function encoding mirrors PVerifier's `[Label]boolean`
  array encoding so the SMT translation stays first-order.
- `machines` — per-`MachineRef` runtime state.
- `actionCount` — global counter bumped on every `send`/`goto`; gives
  every label a unique `actionCount` (the temporal witness for
  `a ≺ b := a.actionCount < b.actionCount`).

Parameterised over the user program's union types through a
`ProgramSig` bundle — `#gen_module` synthesises a concrete `ProgramSig`
per pmodule; hand-written examples construct one directly.
-/
import PLean.Semantics.Label

namespace PLean

/-- A program's union types. `#gen_module` synthesises this from the
registry; hand-written examples populate it directly. -/
structure ProgramSig where
  /-- Event payload union. -/
  E : Type
  /-- Goto payload union. -/
  G : Type
  /-- State-tag enum union (across all machines). -/
  S : Type
  /-- Machine fields (var block) union. -/
  F : Type

namespace ProgramSig

variable (P : ProgramSig)

/-- The label type for this program. -/
abbrev Label := PLean.Label P.E P.G

/-- The per-machine runtime state for this program. -/
abbrev MachineState := PLean.MachineState P.S P.F

end ProgramSig

/-- The global state of a P system, generic over the program's unions.
The `actionCount` field is the temporal witness used by every
send/goto (and by the `≺` operator). -/
structure GlobalState (P : ProgramSig) where
  /-- Characteristic function over labels that have ever been sent. -/
  sent        : P.Label → Bool
  /-- Set of labels that have been delivered (consumed by a handler). -/
  received    : P.Label → Bool
  /-- Per-machine state map. -/
  machines    : MachineRef → P.MachineState
  /-- Global action counter. Incremented on every send/goto so that
      every label gets a unique `actionCount` (the temporal witness
      for `a ≺ b := a.actionCount < b.actionCount`). -/
  actionCount : Nat

namespace GlobalState

variable {P : ProgramSig}

/-- The empty buffer / no-action initial state, given a function for
machine initialisation. Used by `init` blocks and tests. -/
def initial (initMachine : MachineRef → P.MachineState) : GlobalState P :=
  { sent := fun _ => false
    received := fun _ => false
    machines := initMachine
    actionCount := 0 }

/-- Add a label to `sent`. Pure update — used by the `send`/`goto`
primitives in `Primitives.lean`. -/
@[inline] def addSent [DecidableEq P.E] [DecidableEq P.G]
    (s : GlobalState P) (lbl : P.Label) : GlobalState P :=
  { s with sent := fun l => decide (l = lbl) || s.sent l }

/-- Mark a label as received (consumed). -/
@[inline] def addReceived [DecidableEq P.E] [DecidableEq P.G]
    (s : GlobalState P) (lbl : P.Label) : GlobalState P :=
  { s with received := fun l => decide (l = lbl) || s.received l }

/-- Bump the action counter. -/
@[inline] def bumpActionCount (s : GlobalState P) : GlobalState P :=
  { s with actionCount := s.actionCount + 1 }

/-- Update a single machine's state. -/
@[inline] def updateMachine
    (s : GlobalState P) (m : MachineRef) (ms : P.MachineState) : GlobalState P :=
  { s with machines := fun r => if r = m then ms else s.machines r }

end GlobalState

/-- A program needs at least one possible `MachineState` value (for the
NonDetT/CCPO machinery, which expects inhabited carriers). Every
non-trivial P program has at least one machine; `#gen_module` derives
this instance and hand-written examples provide it directly. -/
instance {P : ProgramSig} [Inhabited P.S] [Inhabited P.F] :
    Inhabited (GlobalState P) where
  default :=
    { sent := fun _ => false
      received := fun _ => false
      machines := fun _ => { stage := false, currentState := default, fields := default }
      actionCount := 0 }

end PLean
