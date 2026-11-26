/-
Pin: container helpers reduce to the shapes SMT prep relies on.

These checks guard the encoding contract that lets `pverify_smt` see
container atoms as boolean combinations of applied uninterpreted
symbols (no quotient structure, no internal recursion). If any of
these defeqs regress, container-using obligations will return
`unknown` from the solver instead of closing.
-/
import PLean.Semantics.Containers
import PLean.Syntax.Containers

open PLean

namespace PLean.Tests.Containers

/-! ## Surface macros: type-level desugaring -/

example : set[Nat] = Set Nat := rfl
example : seq[Nat] = List Nat := rfl
example : map[Nat, Nat] = PMap Nat Nat := rfl
example : map[Nat, Nat] = (Nat → Option Nat) := rfl
example : option[Nat] = Option Nat := rfl

/-! ## Map helpers reduce by `simp` to `if`-lambdas -/

example (m : Nat → Option Nat) (k : Nat) (v : Nat) :
    mapInsert m k v = fun x => if x = k then some v else m x := by
  rfl

example (m : Nat → Option Nat) (k : Nat) :
    mapErase m k = fun x => if x = k then none else m x := by
  rfl

example (m : Nat → Option (Set Nat)) (k : Nat) (f : Set Nat → Set Nat) :
    mapModify m k f = fun x => if x = k then (m x).map f else m x := by
  rfl

example : mapEmpty (K := Nat) (V := Nat) = fun _ => none := rfl

/-! ## Lookup-after-mutation lemmas -/

example (m : Nat → Option Nat) (k v : Nat) :
    (mapInsert m k v) k = some v :=
  mapInsert_eq m k v

example (m : Nat → Option Nat) (k k' v : Nat) (h : k' ≠ k) :
    (mapInsert m k v) k' = m k' :=
  mapInsert_ne m k k' v h

example (m : Nat → Option Nat) (k : Nat) :
    (mapErase m k) k = none :=
  mapErase_eq m k

example (m : Nat → Option (Set Nat)) (k : Nat) (f : Set Nat → Set Nat) :
    (mapModify m k f) k = (m k).map f :=
  mapModify_eq m k f

example (m : Nat → Option Nat) (k v v' : Nat) :
    mapInsert (mapInsert m k v) k v' = mapInsert m k v' :=
  mapInsert_mapInsert m k v v'

example (m : Nat → Option Nat) (k v : Nat) :
    mapErase (mapInsert m k v) k = mapErase m k :=
  mapErase_mapInsert m k v

/-! ## Membership round-trips through the `pverifySimp` set

After `simp only [pverifySimp]`, a `Set`-mutation goal becomes a
boolean combination of element equalities and the underlying
predicate applications. Both shapes round-trip through cvc5 (see
`Tests/Semantics/SmtVeilRecipe.lean` for `GlobalState.addSent`'s
analogous shape). -/

example (s : Set Nat) (x e : Nat) :
    x ∈ Insert.insert e s ↔ x = e ∨ x ∈ s := by
  simp only [pverifySimp]

example (s : Set Nat) (x e : Nat) :
    x ∈ s \ Set.singleton e ↔ x ∈ s ∧ x ≠ e := by
  simp only [pverifySimp]
  tauto

end PLean.Tests.Containers
