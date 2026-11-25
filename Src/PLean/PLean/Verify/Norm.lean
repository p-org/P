/-
PLean.Verify.Norm — the `pverifySimp` rules for the transition model.

After `vcgen`, a handler obligation says `Inv (gcommit g m hs')` for a
concrete final handler state `hs'`. These rules unfold `commit` and the
logs, and push structure projections through the resulting `if`s, so the
goal becomes `if`-terms over the pre-state's functions.
-/
import PLean.Semantics.Theorems
import PLean.Verify.SimpAttrs

namespace PLean

variable {S E F G W : Type}

attribute [pverifySimp] gcommit commit logAfter GWorld.consume World.consume HState.start
  Log.push Log.startingAt Log.empty MachineState.gotoState HState.tick

section
variable {c : Prop} [Decidable c] (a b : MachineState S E F G W)
@[pverifySimp] theorem MachineState.state_ite :
    (if c then a else b).state = if c then a.state else b.state := by split <;> rfl
@[pverifySimp] theorem MachineState.fields_ite :
    (if c then a else b).fields = if c then a.fields else b.fields := by split <;> rfl
@[pverifySimp] theorem MachineState.pending_ite :
    (if c then a else b).pending = if c then a.pending else b.pending := by split <;> rfl
@[pverifySimp] theorem MachineState.waiting_ite :
    (if c then a else b).waiting = if c then a.waiting else b.waiting := by split <;> rfl
@[pverifySimp] theorem MachineState.halted_ite :
    (if c then a else b).halted = if c then a.halted else b.halted := by split <;> rfl
end

@[pverifySimp] theorem MachineState.default_state [Inhabited F] :
    (default : MachineState S E F G W).state = none := rfl
@[pverifySimp] theorem MachineState.default_halted [Inhabited F] :
    (default : MachineState S E F G W).halted = false := rfl

/-- An empty index range: what a handler that spawned nothing / sent nothing
leaves in `commit`. -/
@[pverifySimp] theorem empty_range (a r : Nat) : ¬ (a ≤ r ∧ r < a) := by omega

attribute [pverifySimp] Nat.add_zero Nat.zero_add ite_true ite_false and_false false_and
  Option.getD_none Nat.max_def

open Lean Elab Tactic in
/-- Split every hypothesis that is (syntactically) a conjunction. -/
elab "pv_split_ands" : tactic => liftMetaTactic fun g => return [← g.casesAnd]

end PLean
