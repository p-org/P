/-
PLean.Semantics.Core — the types shared by every P program: machine
references, runtime errors, machine-local state and packets.

The program-specific types are type parameters: `S` state tags (a tag
determines its machine kind), `E` events (one constructor per event,
carrying its payload), `F` machine fields, `G` entry arguments, and `W`
suspended `receive`s (one constructor per site, carrying its saved locals).
-/
import PLean.Semantics.Log
import PLean.Semantics.Containers

namespace PLean

/-- A machine reference: an index into the world's machine table. Reference
`0` is P's `null` (the `default` of a machine-typed value); machines are
allocated from `1`. -/
abbrev MachineRef := Nat

/-- P's `null` machine reference. -/
abbrev MachineRef.null : MachineRef := 0

/-- Runtime errors. Every error is a property violation the verifier must
rule out. -/
inductive PError where
  | assertFail (id : Nat)
  | keyNotFound
  | duplicateKey
  | emptyChoose
  | badChoose
  | unhandled
  /-- `send` to `null`. -/
  | nullSend
  /-- A sequence index out of range. -/
  | outOfRange
  deriving Repr, DecidableEq, Inhabited

/-- Machine-local state (Verdi's `State n`). `state = none` means the
reference is unallocated. -/
structure MachineState (S E F G W : Type) where
  state   : Option S
  fields  : F
  /-- The current state's entry still has to run, with this argument (set by
  `goto` and `new`; it runs before the machine dequeues again). -/
  pending : Option G
  waiting : Option W
  halted  : Bool

/-- A packet in flight. -/
structure Packet (E : Type) where
  src : MachineRef
  dst : MachineRef
  ev  : E

instance {S E F G W : Type} [Inhabited F] : Inhabited (MachineState S E F G W) :=
  ⟨{ state := none, fields := default, pending := none, waiting := none, halted := false }⟩

/-- A freshly allocated machine in state `s` whose entry runs with `arg`. -/
@[inline] def MachineState.fresh {S E F G W : Type} [Inhabited F] (s : S) (arg : G) :
    MachineState S E F G W :=
  { state := some s, fields := default, pending := some arg, waiting := none,
    halted := false }

end PLean
