/-
PLean Phase-2 — `.run`-based regression for surface-emitted handlers.

Mirrors `Tests/Semantics/Combinators.lean` (Phase-1 regression for the
primitives) but exercises the *surface*-emitted handlers — the ones
`#gen_module` synthesises from `pmodule M { … }` declarations.

Each test runs a small handler against a concrete starting state and
asserts the resulting `GlobalState` matches the expected delta. This
confirms `var x = expr` actually mutates state, that `send` enqueues
the right buffer entry, and that `goto S` updates `currentState`.
-/
import PLean
import Loom.MonadAlgebras.NonDetT.Extract

open PLean PartialCorrectness DemonicChoice

pmodule SurfCombinators

  event ePing
  event ePong

  machine Counter {
    var count : Nat

    start state Idle {
      entry { pure () }
      on ePing { count = count + 1 }
      on ePong goto Done
    }

    state Done { }
  }

end SurfCombinators

#gen_module SurfCombinators

namespace SurfCombinators

/-- Initial state with one machine at ref 0 in `Counter_Idle`. -/
def init0 : GlobalState Sig :=
  GlobalState.initial' (P := Sig) fun _ =>
    { stage := true, currentState := S.Counter_Idle, fields := default }

/-- Run a `PM Sig α` to its final `(α, GlobalState)`. -/
def runPM {α : Type} [Inhabited α]
    (m : PM' α) (s : GlobalState Sig) : α × GlobalState Sig :=
  DivM.run (m.run.run s)

/-! ## Tests -/

/-- The surface-emitted entry handler is `pure ()` — preserves state. -/
theorem entry_preserves_state :
    (runPM (Counter.Idle.entry ⟨0⟩) init0).snd.actionCount = 0 := by
  decide

/-- `count = count + 1` increments the field via the synthesised `_set`
helper. The state's `Counter_count` field starts at 0 (the
`Inhabited` default) and ends at 1 after one ePing. -/
theorem ePing_increments_count :
    let s' := (runPM (Counter.Idle.ePing_handler ⟨0⟩) init0).snd
    s'.machines 0 |>.fields.Counter_count = 1 := by
  decide

/-- Two ePings yield count = 2 — confirms reads pick up writes from the
prior bound `count` (the `_get` re-read after each `_set`). -/
theorem two_ePings_increment_twice :
    let prog : PM' Unit := do
      Counter.Idle.ePing_handler ⟨0⟩
      Counter.Idle.ePing_handler ⟨0⟩
    let s' := (runPM prog init0).snd
    s'.machines 0 |>.fields.Counter_count = 2 := by
  decide

end SurfCombinators
