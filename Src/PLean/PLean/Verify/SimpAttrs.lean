/-
PLean.Verify.SimpAttrs — attributes `#pverify` reads:

* `pverifySimp`, the simp set that normalises a handler obligation after
  `vcgen` (unfold `commit`, push projections through `if`s, …) before it
  reaches `crush`;
* `pverifyProof`, which marks a manual proof of an obligation.

An attribute must be registered in a different file than any use.
-/
import Lean

register_simp_attr pverifySimp

namespace PLean

open Lean in
/-- `@[pverifyProof] theorem <obligation> : <its statement> := …`, after
`#gen_module M` and before `#pverify M` (inside `namespace M`), proves an
obligation by hand: `#pverify` checks the statement and uses the theorem
instead of the solver. -/
initialize pverifyProofAttr : TagAttribute ←
  registerTagAttribute `pverifyProof "a manual proof of a `#pverify` obligation" fun decl => do
    unless ((← getEnv).find? decl).any (· matches .thmInfo _) do
      throwError "`@[pverifyProof]`: `{decl}` is not a theorem; a manual proof must be one"

end PLean
