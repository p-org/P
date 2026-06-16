/-
PLean Phase-1 Task 1 — monad-stack spike.

Goal: confirm that the proposed Phase-1 monad stack
  `PM α := NonDetT (StateT GlobalState DivM) α`
synthesises every typeclass instance the verification pipeline needs,
once `PartialCorrectness DemonicChoice` is open. This file is the
de-risk gate: if anything below fails to elaborate, the layer order
or `open` set in the Phase-1 plan is wrong.

Throwaway state shape; the real `GlobalState` is built in Task 3.

Key finding (universes): the `abbrev`s must NOT carry an explicit
`: Type` annotation. `NonDetT m α` lives in `Type _` (Lean picks the
universe), and pinning `PM`'s codomain to `Type` causes a universe
mismatch that surfaces as a misleading `failed to synthesize Monad PM`.
-/
import Loom.MonadAlgebras.NonDetT.Basic
import Loom.MonadAlgebras.Instances.StateT
import Loom.MonadAlgebras.Instances.Basic
import Loom.MonadAlgebras.WP.Basic
import Loom.MonadAlgebras.WP.Tactic

namespace PLean.Spike

/-- Throwaway state for the spike. -/
structure SpikeState where
  counter : Nat
  flag    : Bool
  deriving Inhabited

/-- The recommended Phase-1 stack: `NonDetT` outermost, `StateT` over
`DivM` underneath. The `MAlgOrdered` instance is derived automatically. -/
abbrev PM (α : Type) := NonDetT (StateT SpikeState DivM) α

/-- Top-level assertion lattice for the stack. `StateT σ` lifts logic
from `l` to `σ → l`; `NonDetT` and `DivM` keep the lattice unchanged.
With `l := Prop` at the bottom, the resulting lattice for `PM` is
`SpikeState → Prop`. -/
abbrev PProp := SpikeState → Prop

/-! ## Layer-by-layer instance synthesis (no `open` needed) -/

#synth Monad DivM
#synth LawfulMonad DivM
#synth Monad (StateT SpikeState DivM)
#synth LawfulMonad (StateT SpikeState DivM)
#synth Monad PM
#synth LawfulMonad PM

/-! ## Verification-mode instances live behind a `scoped` `open` -/

open PartialCorrectness DemonicChoice

#synth MAlgOrdered DivM Prop
#synth MAlgOrdered (StateT SpikeState DivM) PProp
#synth MAlgOrdered PM PProp

/-! ## Tiny triple proofs

The point of these is not to prove anything interesting — it's to
exercise `wpgen` end-to-end on the chosen stack. -/

/-- A `pure ()` action satisfies any post-condition that holds in every
state. Trivial, but it confirms the WPGen plumbing wires up. -/
example : triple (l := PProp)
    (fun _ => True)
    (pure () : PM Unit)
    (fun _ _ => True) := by
  wpgen
  intro _
  trivial

/-- A `pure x` triple that observes the result. -/
example : triple (l := PProp)
    (fun _ => True)
    (pure 42 : PM Nat)
    (fun n _ => n = 42) := by
  wpgen
  intro _ _
  rfl

/-- A bind chain through `pure`. -/
example : triple (l := PProp)
    (fun _ => True)
    (do let _ ← (pure 1 : PM Nat); pure ())
    (fun _ _ => True) := by
  wpgen
  intro _
  trivial

end PLean.Spike
