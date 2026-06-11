/-
PLean.Verify.SimpAttrs — register the `pverifySimp` simp attribute.

Lives in its own file because `register_simp_attr` needs to land
*before* any `@[pverifySimp]` use site is elaborated. Veil follows
the same split: `Veil/Base.lean` registers `smtSimp` /
`logicSimp` / `quantifierSimp`, while `Veil/SMT/Preparation.lean`
populates them in a separately-imported file.

The attribute's purpose is documented in `Verify/Tactic.lean`. -/
import Lean

/-- Simp set used by `pverify_smt_prep` to massage a goal into a
form lean-auto can translate. Contains:
- `funextEq` (a simproc): rewrites `f = g` (functions) into
  pointwise `∀ x, f x = g x`.
- `iff_eq_eq`, `tupleEq`, `tupleForall`, `tupleExists`: structural
  normalisations Veil ships in its `smtSimp` set.
- `GlobalState.addSent` / `addReceived` / `bumpActionCount` /
  `updateMachine`: β-reduce post-state record updates so
  field projections become applied uninterpreted symbols.
- `inflight`, `sent`, `received`: unfold the buffer-state predicates.

See `Verify/Tactic.lean` for the lemma definitions and the tactics
that consume this set. -/
register_simp_attr pverifySimp
