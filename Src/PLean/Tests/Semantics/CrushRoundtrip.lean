/-
`crush` smoke test: the solver is reachable and goals close with a
checked proof (`crush.trust "reconstruct"`).
-/
import Crush

set_option crush.backend "cvc5"
set_option crush.trust "reconstruct"
set_option crush.reconstruct "auto"

namespace PLean.CrushRoundtrip

/-! ## Probe 1: solver reachability -/

theorem reach (x : Nat) : x = x := by
  crush

/-! ## Probe 2: quantified linear arithmetic -/

theorem add_comm' : ∀ a b : Nat, a + b = b + a := by
  crush

/-! ## Probe 3: existential over an uninterpreted predicate -/

theorem exists_weaken (P : Nat → Prop) (h : ∃ a, P a ∧ a < 10) :
    ∃ a, P a ∧ a < 100 := by
  crush

/-! ## Probe 4: lambda update of a function-valued component -/

theorem consume_other (net : Nat → Option Nat) (i j : Nat) (hj : j ≠ i) :
    (fun k => if k = i then none else net k) j = net j := by
  crush

end PLean.CrushRoundtrip
