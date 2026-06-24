/-
Regression: `pinstance` reaches SMT.

PLean's `pinstance <id> : <Class> <T>` (Veil-style "instantiate") used
to elaborate as a single `variable [<id> : <Class> <T>]`, with two
SMT-visibility gaps:

1. **Auto-bound generalisation.** A later
   `invariant P : <body using Class.field>` got the type
   `[ord : Class T] → GS → Prop`, not `GS → Prop`, so the bundle
   `def L : GS → Prop := fun s => P s ∧ True` failed to typecheck and
   the obligation skeleton rendered as `sorry ∧ True`.
2. **Lctx invisibility.** Even with the bundle fixed, the class's
   stated axioms (e.g. `TotalOrder.le_refl`) wouldn't reach SMT —
   `loom_smt [*]` collects only the local context.

The current implementation closes both: `materialiseInstance` emits a
`noncomputable axiom <id> : <Class> <T>` plus an anonymous
`instance : <Class> <T> := <id>` so `<Class>.<field>` resolves
without an explicit binder; and `synthInstanceFieldAxioms` (called by
`#gen_module`) walks the class's Prop-typed fields and emits one
top-level `noncomputable def <id>_<field> := @<Class>.<field> _ <id>`
per axiom field, registering each in `ctx.axioms` so the obligation
generator's existing `have hax_<name>` injection brings them into
every VC's local context.

This file pins both gaps closed: `base_block0_always_self`
(`∀ x : N, TotalOrder.le x x = true` from init) discharges by SMT
using the synthesised `ord_le_refl` axiom. Pre-fix the base case was
reported `disproved`.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

class TotalOrder (t : Type) where
  le : t → t → Bool
  le_refl  : ∀ x : t, le x x = true
  le_trans : ∀ x y z : t, le x y = true → le y z = true → le x z = true
  le_total : ∀ x y : t, le x y = true ∨ le y x = true

pmodule InstanceExercise

  type N

  pinstance ord : TotalOrder N

  event ePing

  machine Server {
    start state S {
      on ePing { pure () }
    }
  }

  Lemma le_self {
    invariant always_self : ∀ x : N, TotalOrder.le x x = true
  }
  Proof {
    prove le_self ;
  }

end InstanceExercise

#gen_module InstanceExercise
#pwf        InstanceExercise

#pverify InstanceExercise
