/-
Regression for `<Mod>.<M>_allocated m s := kind ≠ 0 ∧ kind = <M>_kind`.
Pins both halves of the conjunction so a future revert to a single
equality is caught by `decide`.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule Phase3R20Mod

  event eFoo

  machine A {
    start state S { on eFoo { pure () } }
  }

  machine B {
    start state T { on eFoo { pure () } }
  }

end Phase3R20Mod

#gen_module Phase3R20Mod

namespace Phase3R20Mod

/-- Initial state where machine 0 has kind = 0 (the `Inhabited`
default — unset). -/
def stateUninit : GlobalState Sig :=
  GlobalState.initial (P := Sig) fun _ => default

/-- A_allocated rejects kind = 0. -/
example : ¬ A_allocated 0 stateUninit := by
  unfold A_allocated
  decide

/-- B_allocated rejects kind = 0 too. -/
example : ¬ B_allocated 0 stateUninit := by
  unfold B_allocated
  decide

/-- Initial state where machine 0 has been explicitly assigned
`A_kind`. -/
def stateInitA : GlobalState Sig :=
  GlobalState.initial (P := Sig) fun _ =>
    { stage := false, currentState := default, fields := default,
      kind := A_kind }

/-- A_allocated accepts a properly-initialised A machine. -/
example : A_allocated 0 stateInitA := by
  unfold A_allocated A_kind
  decide

/-- A_allocated still rejects a machine whose kind is the
*other* machine's kind (B_kind). -/
def stateInitB : GlobalState Sig :=
  GlobalState.initial (P := Sig) fun _ =>
    { stage := false, currentState := default, fields := default,
      kind := B_kind }

example : ¬ A_allocated 0 stateInitB := by
  unfold A_allocated A_kind stateInitB
  decide

end Phase3R20Mod
