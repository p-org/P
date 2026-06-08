/-
PLean Phase-3 — regression for the PLAN_P3 R20 mitigation
(REVIEW_P3 second-pass §2.6).

`<Mod>.<M>_allocated m s` was strengthened from
`kind = <M>_kind` to `kind ≠ 0 ∧ kind = <M>_kind`. This file pins
both halves of the conjunction:
  (1) a machine ref whose `kind` field is the default value `0`
      does NOT satisfy `<M>_allocated` (kind ≠ 0 half),
  (2) a machine ref whose `kind` field is set to `<M>_kind` DOES
      satisfy `<M>_allocated` (the equality half).

Both checks use `decide` on a concrete `GlobalState`; if the
predicate body is reverted to a single equality the (1) check
fails and CI breaks.
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
default — unset per R20). -/
def stateUninit : GlobalState Sig :=
  GlobalState.initial (P := Sig) fun _ => default

/-- A_allocated rejects kind = 0 (R20 first half). -/
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

/-- A_allocated accepts a properly-initialised A machine
(equality half). -/
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
