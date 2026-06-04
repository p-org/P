/-
PLean.Semantics.GlobalState — the global system state.

Mirrors PVerifier's `StateAdt` at `Uclid5CodeGenerator.cs:594-606`:
```
type StateAdt = record {
  sent     : [Label]boolean,
  received : [Label]boolean,
  machines : [MachineRef]MachineStateAdt
};
```
plus the top-level `actionCount : integer` from `:1119`. The Lean
encoding folds `actionCount` into the record.

`sent`/`received` are encoded as `Label → Bool` (matching PVerifier's
`[Label]boolean` exactly), not as `Set Label`, to stay close to the
PVerifier SMT encoding (decision D4).

The whole thing is parameterised over the user program's union types
through a `ProgramSig` bundle — Phase 2's `#gen_module` synthesises a
concrete `ProgramSig` per pmodule, and Phase-1 hand-written examples
construct one directly.
-/
import PLean.Semantics.Label

namespace PLean

/-- A program's union types. Phase 2 will synthesise this from the
registry. Phase 1 hand-written examples (e.g., `HandPingPong.lean`)
populate it directly. -/
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

Mirrors PVerifier's `StateAdt` exactly. The `actionCount` field is the
temporal witness used by every send/goto (and by the `≺` operator). -/
structure GlobalState (P : ProgramSig) where
  /-- Set of all labels that have ever been sent. Encoded as a
      characteristic function, matching PVerifier's `[Label]boolean`. -/
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
non-trivial P program has at least one machine, so this is provided
by hand-written examples and synthesised by Phase 2. -/
instance {P : ProgramSig} [Inhabited P.S] [Inhabited P.F] :
    Inhabited (GlobalState P) where
  default :=
    { sent := fun _ => false
      received := fun _ => false
      machines := fun _ => { stage := false, currentState := default, fields := default }
      actionCount := 0 }

end PLean
