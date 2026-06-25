/-
PLean.Semantics.Loop — loop primitives for P's `foreach` and `while`.

P's surface forms:

```
  foreach (x in xs) invariant N1 : I1; … invariant Nk : Ik; { body }
  while (cond)      invariant N1 : I1; … invariant Nk : Ik; { body }
```

`while` desugars to Lean's `for _ in Lean.Loop.mk do …`, which Loom's
`WPGen.forWithInvariantLoop` (`@[loomSpec]`) already matches.

`foreach` desugars to the PLean-local primitive `pforeach`. Loom only
ships WPGen specs for `Std.Range`-shaped and `Lean.Loop.mk`-shaped
`forIn`; a list-shape spec is the missing piece. We provide it here
via `WPGen.pforeach` (`@[loomSpec]`), proven by reduction to Loom's
`triple_forIn_list` with a constant-in-remaining-list invariant
(every PLean loop invariant is a state-implicit predicate, not a
remaining-list-dependent one).

`loopInvariantGadget` is a PLean-local alias for Loom's
`invariantGadget` — keeps the surface decoupled from Loom's gadget
machinery.
-/
import Loom.MonadAlgebras.WP.Gen
import PLean.Semantics.Monad

namespace PLean

open PartialCorrectness DemonicChoice in
/-- Loop-invariant marker; semantically `pure .unit`. The WPGen
machinery on `pforeach` / `WPGen.forWithInvariantLoop` reads the
invariant list from this gadget call. -/
@[reducible] noncomputable def loopInvariantGadget
    {P : ProgramSig}
    (inv : List (PProp P)) : PM P PUnit :=
  invariantGadget (m := PM P) (l := PProp P) inv

/-! ## `pforeach` — list-iteration primitive.

The surface `foreach (x in xs) invariant N : I; { body }` desugars to

```
  pforeach xs [I1, …, Ik] (fun x => do body)
```

`pforeach`'s body itself runs `invariantGadget inv; body x; pure
(ForInStep.yield ())` per iteration — the `invariantGadget` is what
makes the WPGen registration possible. -/

open PartialCorrectness DemonicChoice in
@[reducible] noncomputable
def pforeach {P : ProgramSig} {α : Type}
    (xs : List α) (inv : List (PProp P))
    (body : α → PM P Unit) : PM P Unit :=
  forIn xs () fun x _ => do
    invariantGadget (m := PM P) inv
    body x
    pure (ForInStep.yield ())

open PartialCorrectness DemonicChoice in
/-- The Hoare-triple form of `pforeach`'s WP: every iteration's
`body x` preserves the conjunction `invariantSeq inv` of the user's
loop invariants; the loop as a whole therefore preserves it.

Specialises Loom's generic `triple_forIn_list` with
`inv := fun _ _ => invariantSeq inv₀` (PLean invariants don't track
the remaining elements, only global state). -/
theorem triple_pforeach {P : ProgramSig} {α : Type}
    (xs : List α) (inv : List (PProp P))
    (body : α → PM P Unit)
    (hstep : ∀ x, triple (invariantSeq inv) (body x)
                          (fun _ => invariantSeq inv)) :
    triple (invariantSeq inv) (pforeach (P := P) xs inv body)
           (fun _ => invariantSeq inv) := by
  unfold pforeach
  apply triple_forIn_list (inv := fun _ _ => invariantSeq inv)
  intro hd _tl _b
  apply triple_bind (cut := fun _ => invariantSeq inv)
  · -- invariantGadget is `pure .unit`; pure preserves any invariant.
    simp only [invariantGadget]
    exact (triple_pure _ _ _).mpr (le_refl _)
  intro _
  apply triple_bind (cut := fun _ => invariantSeq inv)
  · exact hstep hd
  intro _
  -- `pure (ForInStep.yield ())` with the match-shaped post that
  -- triple_forIn_list expects. Both match branches reduce to
  -- `invariantSeq inv` (we instantiated with constant `inv`), so
  -- `triple_pure` closes the residue.
  exact (triple_pure _ _ _).mpr (le_refl _)

open PartialCorrectness DemonicChoice in
/-- WPGen for `pforeach`. Same shape as Loom's
`WPGen.forWithInvariantLoop` (Lean.Loop.mk version): the get carries
the iteration condition as an embedded `⌜⌝`, conjoined with the
`spec` form of "the loop preserves the invariant conjunction".

Discrimination-tree key for `loomSpec` registration is the
`pforeach` head — discriminates from the `forIn`-on-Std.Range and
`forIn`-on-Lean.Loop forms Loom already has, so the three loop
WPGens don't shadow each other. -/
@[loomSpec, loomWpSimp]
noncomputable
def WPGen.pforeach {P : ProgramSig} {α : Type}
    (xs : List α) (inv : List (PProp P))
    (body : α → PM P Unit)
    (wpg : ∀ x, WPGen (body x)) :
    WPGen (PLean.pforeach (P := P) xs inv body) where
  get post :=
    ⌜∀ x, invariantSeq inv ≤ (wpg x).get (fun _ => invariantSeq inv)⌝ ⊓
    spec (invariantSeq inv) (fun _ => invariantSeq inv) post
  prop := by
    intro post
    simp only [LE.pure]
    split_ifs with h <;> try simp
    apply (triple_spec _ _ _).mpr
    apply triple_pforeach
    intro x
    apply (wpg x).intro
    exact h x

end PLean
