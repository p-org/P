/-
PLean.Semantics.Monad — the P monad over Loom.

  `PM P α := NonDetT (StateT (GlobalState P) DivM) α`

with assertion lattice `PProp P := GlobalState P → Prop`. P's
safety-only verification doesn't need an `ExceptT` layer on top.

`NonDetT`-outermost is load-bearing for the partial-correctness +
demonic-choice mode the verifier targets — the lattice instances live
under `scoped` in `PartialCorrectness DemonicChoice`. Importers reach
the instances via the standard preamble:

  ```lean
  import PLean.Semantics.Monad
  open PLean PartialCorrectness DemonicChoice
  ```
-/
import Loom.MonadAlgebras.NonDetT.Basic
import Loom.MonadAlgebras.Instances.StateT
import Loom.MonadAlgebras.Instances.Basic
import Loom.MonadAlgebras.WP.Basic
import Loom.MonadAlgebras.WP.Tactic
import PLean.Semantics.GlobalState

namespace PLean

/-- The P monad: nondeterministic state-passing over divergence. -/
abbrev PM (P : ProgramSig) (α : Type) :=
  NonDetT (StateT (GlobalState P) DivM) α

/-- The P assertion lattice: predicates over the global state. -/
abbrev PProp (P : ProgramSig) := GlobalState P → Prop

/-! ## Instance check (compile-time): every layer of the stack
synthesises its `MAlgOrdered` instance once `PartialCorrectness
DemonicChoice` is open. We instantiate the program signature with the
trivial all-`Unit` shape so this check has zero runtime cost. -/

namespace Internal

/-- Trivial program signature used only for the synthesis assertion below. -/
private def TrivialSig : ProgramSig :=
  { E := Unit, G := Unit, S := Unit, F := Unit }

open PartialCorrectness DemonicChoice in
noncomputable example : MAlgOrdered (PM TrivialSig) (PProp TrivialSig) :=
  inferInstance

end Internal

end PLean
