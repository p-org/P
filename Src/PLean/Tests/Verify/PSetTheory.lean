/-
Pins the `PSet` first-order set theory (`Semantics/PSet.lean`):

1. **No new axioms.** The theory is model-backed; `#print axioms` on
   each spec lemma shows only Lean's foundational axioms.
2. **Membership + Eq discharges through SMT.** The reifter-bug shape
   (`x ∈ s` with `x = a` in play) that the transparent `Set` hits
   translates and closes once membership is the opaque `PSet.mem`.
3. **Whole-set predicate + intersection axiom discharges.** Models
   Consensus's `isQuorum : set[T] → Bool` + `quorum_intersect`: a set
   passed as a value round-trips as an uninterpreted sort.
-/
import PLean.Semantics.PSet
import Loom.SMT
import Loom.MonadAlgebras.WP.Options
import PLean.Verify.SimpLemmas

open PLean

set_option crush.backend "cvc5"

namespace PLean.Tests.PSetTheory

/-! ## No new axioms -/

-- If any spec lemma silently became an axiom, these would list it.
-- All hold by `rfl`/`Iff.rfl`/`id` over the model, so none depend on
-- any axiom — the trusted base is untouched.
/-- info: 'PLean.PSet.mem_insert' does not depend on any axioms -/
#guard_msgs in #print axioms PSet.mem_insert
/-- info: 'PLean.PSet.mem_empty' does not depend on any axioms -/
#guard_msgs in #print axioms PSet.mem_empty
/-- info: 'PLean.PSet.mem_erase' does not depend on any axioms -/
#guard_msgs in #print axioms PSet.mem_erase

/-! ## Membership + Eq — the reifier-bug shape, now first-order -/

example (s : PSet Nat) (a x : Nat) (h : x ∈ s) (heq : x = a) : a ∈ s := by
  simp only [pverifySimp] at *
  crush [h, heq]

example (s : PSet Nat) (a b : Nat) (h : b ∈ s) : b ∈ PSet.insert a s := by
  simp only [pverifySimp] at *
  crush [h]

example (a : Nat) : a ∈ PSet.insert a (PSet.empty : PSet Nat) := by
  simp only [pverifySimp] at *
  loom_smt

example (s : PSet Nat) (a b : Nat) (h : b ∈ s) (hne : b ≠ a) :
    b ∈ PSet.erase a s := by
  simp only [pverifySimp] at *
  crush [h, hne]

/-! ## Whole-set value pass + intersection axiom (the Consensus shape) -/

opaque isQuorum {α : Type} : PSet α → Bool

axiom quorum_intersect {α : Type} (q1 q2 : PSet α) :
    isQuorum q1 = true → isQuorum q2 = true → ∃ a : α, a ∈ q1 ∧ a ∈ q2

example (q1 q2 : PSet Nat) (h1 : isQuorum q1 = true) (h2 : isQuorum q2 = true) :
    ∃ a : Nat, a ∈ q1 ∧ a ∈ q2 := by
  have := quorum_intersect q1 q2 h1 h2
  crush [this]

end PLean.Tests.PSetTheory
