/-
PLean.Verify.SimpAttrs — the `pverifySimp` simp set: the rewrite rules
that normalise a handler obligation after `vcgen` (unfold `commit`, push
projections through `if`s, …) before it reaches `crush`.

`register_simp_attr` must be elaborated in a different file than any use.
-/
import Lean

register_simp_attr pverifySimp
