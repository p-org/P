/-
PLean Phase-1 Task 9 — `.run`-based regression for primitives.

Each test runs a small `PM` program against a concrete starting state
and asserts the resulting `GlobalState` matches the expected delta.
This is the executable counterpart of M1's triples — confirming the
primitives compute the buffer/counter updates we proved them to
preserve.

Mirrors Cashmere's `#eval (prog args).run.run.run initState` idiom,
adapted for our two-transformer stack: the `.run.run.run` chain
unwraps `NonDetT → StateT → DivM`; the final `DivM` value is
destructured via `DivM.run` (`Inhabited`-defaulted on divergence,
which the terminating test programs never trigger).

Tests are `theorem`s (not `example`s) because `decide`'s reflection
step generates auxiliary `match_*` constants and needs a stable
declaration name to attach them to. `example`s would compile but
the IDE-side `nativeDecide` path complains about the missing
`_example.match_*` constants.
-/
import PLean.Semantics.Monad
import PLean.Semantics.Primitives
import Loom.MonadAlgebras.NonDetT.Extract

namespace PLean.CombinatorTests

/-! ## A trivial program signature for unit tests -/

inductive Ev where
  | ePing
  | ePong
  deriving Inhabited, DecidableEq, Repr

inductive GotoP where
  | unit
  deriving Inhabited, DecidableEq, Repr

inductive St where
  | A
  | B
  deriving Inhabited, DecidableEq, Repr

abbrev Sig : ProgramSig :=
  { E := Ev, G := GotoP, S := St, F := Unit }

abbrev M' (α : Type) := PM Sig α

/-- Concrete starting state with one machine at ref 0 in state A. -/
def init0 : GlobalState Sig :=
  GlobalState.initial (P := Sig) fun _ =>
    { stage := true, currentState := St.A, fields := () }

/-- Run a `PM Sig α` to its final `(α, GlobalState)` pair, projecting
out of `DivM` with `default` on divergence. The test programs all
terminate, so the `default` branch is never taken. -/
def runPM {α : Type} [Inhabited α]
    (m : M' α) (s : GlobalState Sig) : α × GlobalState Sig :=
  DivM.run (m.run.run s)

/-! ## Tests -/

/-- `pure` is the identity on state. -/
theorem pure_preserves_state :
    (runPM (pure () : M' Unit) init0).snd.actionCount = 0 := by
  decide

/-- `send` enqueues exactly one label and bumps the counter. -/
theorem send_enqueues_one :
    let s' := (runPM (send (P := Sig) 1 Ev.ePing) init0).snd
    s'.actionCount = 1 ∧
    s'.sent { target := 1, action := .event Ev.ePing, actionCount := 0 } = true := by
  decide

/-- Two consecutive `send`s yield two distinct counters. -/
theorem send_send_increments_twice :
    let prog : M' Unit := do
      send (P := Sig) 1 Ev.ePing
      send (P := Sig) 1 Ev.ePong
    (runPM prog init0).snd.actionCount = 2 := by
  decide

/-- `markReceived` updates `received` without touching `sent` or the
counter. -/
theorem markReceived_only_touches_received :
    let lbl : Sig.Label := { target := 0, action := .event Ev.ePing, actionCount := 7 }
    let s' := (runPM (markReceived (P := Sig) lbl) init0).snd
    s'.received lbl = true ∧ s'.actionCount = 0 := by
  decide

end PLean.CombinatorTests
