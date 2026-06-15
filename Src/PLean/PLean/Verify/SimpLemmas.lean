/-
PLean.Verify.SimpLemmas — pre-SMT simplification set for `pverify_smt_close`.

Houses every lemma / simproc tagged into the `pverifySimp` attribute
(registered in `Verify/SimpAttrs.lean`). The set turns higher-order
constructs (function-typed record fields, function/iff/tuple
equalities) into first-order forms `lean-auto` can translate. After
`simp only [pverifySimp]`:
- function-typed values appear only in *applied* form (so
  `s.sent : Label → Bool` is translated as an uninterpreted function);
- iff and tuple equalities are normalised to plain `=`/`∧`;
- `GlobalState` post-state record updates β-reduce, so field
  projections become applied uninterpreted symbols.

Tactics that consume this set live in `Verify/Tactic.lean`
(`pverify_smt_prep` / `pverify_smt_close`).
-/
import Lean
import Loom.MonadAlgebras.WP.Tactic
import PLean.Semantics.GlobalState
import PLean.Semantics.Predicates
import PLean.Verify.SimpAttrs

/-- Function-extensional equality. After this rewrite fires,
function-typed values never appear as SMT atoms; only their *applied*
forms do, which lean-auto translates as uninterpreted function symbols.
This is what lets `Label → Bool` / `MachineRef → MachineState` record
fields go through SMT without refactoring `GlobalState` itself. -/
theorem PLean.funextEq' {α β : Type} (f g : α → β) :
    (f = g) = ∀ x, f x = g x := by
  apply propext
  constructor
  · intro h; simp only [h, implies_true]
  · intro h; apply funext h

open Lean Expr Meta in
/-- Simproc form of `funextEq'` that fires whenever both sides of an
equality have a function type. -/
simproc ↓ funextEq (_ = _) :=
  fun e => do
    let_expr Eq _ lhs rhs := e | return .continue
    let lhsT ← inferType lhs
    if lhsT.isArrow && (← inferType rhs).isArrow then
      let bn ← Lean.Meta.getUnusedUserName `a
      let bt := lhsT.bindingDomain!
      let nlhs := app lhs (bvar 0)
      let nrhs := app rhs (bvar 0)
      let qexpr := forallE bn bt (← mkEq nlhs nrhs) BinderInfo.default
      let proof ← mkAppM ``PLean.funextEq' #[lhs, rhs]
      return .visit { expr := qexpr, proof? := proof }
    return .continue
attribute [pverifySimp] funextEq

/-- `(p ↔ q) = (p = q)` — lean-auto can choke on `↔`; this rewrite
makes the goal use `=` only. -/
@[pverifySimp] theorem PLean.iff_eq_eq {p q : Prop} : (p ↔ q) = (p = q) :=
  propext ⟨propext, (· ▸ ⟨(·), (·)⟩)⟩

/-- Tuple equality unfolds to per-component equality (tuples are not
native SMT-LIB sorts). -/
@[pverifySimp] theorem PLean.tupleEq {α β : Type}
    [DecidableEq α] [DecidableEq β] (a c : α) (b d : β) :
    ((a, b) = (c, d)) = (a = c ∧ b = d) := by
  apply propext; constructor
  · intro h; injection h; constructor <;> assumption
  · rintro ⟨h1, h2⟩; rw [h1, h2]

/-- Destruct a quantifier over tuples into per-component quantifiers. -/
@[pverifySimp] theorem PLean.tupleForall {α β : Type} {P : α × β → Prop} :
    (∀ x : α × β, P x) = (∀ a : α, ∀ b : β, P (a, b)) := by
  apply propext; constructor
  · rintro h a b; exact h (a, b)
  · rintro h ⟨a, b⟩; exact h a b

/-- Mirror of `tupleForall` for existentials. -/
@[pverifySimp] theorem PLean.tupleExists {α β : Type} {P : α × β → Prop} :
    (∃ x : α × β, P x) = (∃ a : α, ∃ b : β, P (a, b)) := by
  apply propext; constructor
  · rintro ⟨⟨a, b⟩, h⟩; exact ⟨a, b, h⟩
  · rintro ⟨a, b, h⟩; exact ⟨⟨a, b⟩, h⟩

-- After `simp only [pverifySimp]`, a goal mentioning `(s.addSent lbl).sent l`
-- becomes `decide (l = lbl) || s.sent l = true`, which lean-auto translates
-- as an applied uninterpreted `s.sent` symbol.
attribute [pverifySimp]
  PLean.GlobalState.addSent
  PLean.GlobalState.addReceived
  PLean.GlobalState.bumpActionCount
  PLean.GlobalState.updateMachine
  PLean.inflight
  PLean.sent
  PLean.received
