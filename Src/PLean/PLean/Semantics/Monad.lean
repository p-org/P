/-
PLean.Semantics.Monad — the P monad.

  `PM P α := StateT (GlobalState P) Id α`

with assertion lattice `PProp P := GlobalState P → Prop`, which is
exactly the `Pred` that `Std.WP`'s `StateT`/`Id` `WPMonad` instances
assign to `PM P`, so `vcgen` applies to handler bodies directly.

INTERIM. This is the deterministic placeholder PLean adopted when it
dropped Loom (whose `PM` was `NonDetT (StateT (GlobalState P) DivM)`).
It is pending the transition-model redesign:
- no nondeterminism layer, so `PLean.choose` is currently unavailable;
- divergence is covered by `Lean.Order.MonadTail` (core provides it
  for `StateT σ Id`), which is what `Std.WP`'s partial-correctness
  `repeat`/`while` loop specs require.
-/
import Std.WP
import PLean.Semantics.GlobalState

namespace PLean

/-- The P monad: state-passing over the global system state. -/
abbrev PM (P : ProgramSig) (α : Type) :=
  StateT (GlobalState P) Id α

/-- The P assertion lattice: predicates over the global state. -/
abbrev PProp (P : ProgramSig) := GlobalState P → Prop

/-! ## Instance check (compile-time): `PM` carries a `Std.WP`
interpretation whose assertion language is `PProp`. -/

namespace Internal

/-- Trivial program signature used only for the synthesis assertion below. -/
private def TrivialSig : ProgramSig :=
  { E := Unit, G := Unit, S := Unit, F := Unit }

open Std.WP in
example : WPMonad (PM TrivialSig) (PProp TrivialSig) EStack⟨⟩ :=
  inferInstance

end Internal

end PLean
