/-
PLean.Verify.SimpAttrs — register the `pverifySimp` simp attribute.

`register_simp_attr` runs at command initialization, so it must be
elaborated in a different file than any `@[pverifySimp]` use site.
The lemmas that populate the set live in `Verify/SimpLemmas.lean`;
the consuming tactics live in `Verify/Tactic.lean`. -/
import Lean

register_simp_attr pverifySimp
