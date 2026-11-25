/-
PLean.Verify.Norm — the `pverifySimp` rules for the transition model.

After `vcgen`, a handler obligation says `Inv (gcommit g m hs')` for a
concrete final handler state `hs'`. These rules unfold `commit` and the
logs, and push structure projections through the resulting `if`s, so the
goal becomes `if`-terms over the pre-state's functions.
-/
import PLean.Semantics.Theorems
import PLean.Verify.SimpAttrs

namespace PLean

variable {S E F G W : Type}

attribute [pverifySimp] Framework gcommit commit logAfter GWorld.consume World.consume HState.start
  Log.push_get Log.push_size Log.startingAt Log.empty MachineState.gotoState HState.tick

section
variable {c : Prop} [Decidable c] (a b : MachineState S E F G W)
@[pverifySimp] theorem MachineState.state_ite :
    (if c then a else b).state = if c then a.state else b.state := by split <;> rfl
@[pverifySimp] theorem MachineState.fields_ite :
    (if c then a else b).fields = if c then a.fields else b.fields := by split <;> rfl
@[pverifySimp] theorem MachineState.pending_ite :
    (if c then a else b).pending = if c then a.pending else b.pending := by split <;> rfl
@[pverifySimp] theorem MachineState.waiting_ite :
    (if c then a else b).waiting = if c then a.waiting else b.waiting := by split <;> rfl
@[pverifySimp] theorem MachineState.halted_ite :
    (if c then a else b).halted = if c then a.halted else b.halted := by split <;> rfl
end

@[pverifySimp] theorem MachineState.default_state [Inhabited F] :
    (default : MachineState S E F G W).state = none := rfl
@[pverifySimp] theorem MachineState.default_halted [Inhabited F] :
    (default : MachineState S E F G W).halted = false := rfl

/-- An empty index range: what a handler that spawned nothing / sent nothing
leaves in `commit`. -/
@[pverifySimp] theorem empty_range (a r : Nat) : ¬ (a ≤ r ∧ r < a) := by omega

attribute [pverifySimp] Nat.add_zero Nat.zero_add ite_true ite_false and_false false_and
  Option.getD_none Nat.max_def true_implies false_implies implies_true Bool.true_eq_false
  Bool.false_eq_true and_true true_and

/-! `vcgen`'s loop gadgets: pointwise lattice operations on predicates, and
the `decreasing` measure. -/
attribute [pverifySimp] Lean.Order.meet_apply Lean.Order.iSup_apply Lean.Order.top_apply
  Lean.Order.CompleteLattice.ofProp_apply Lean.Order.CompleteLattice.ofProp_prop_eq
  Lean.Order.meet_prop_eq_and Lean.Order.iSup_prop_eq_exists Lean.Order.top_prop_eq
  Std.WP.Variant.evalsTo_ofMeasure Std.WP.Assertion.NondetFun.evalsTo_pure Std.WP.Assertion.NondetFun.evalsTo_apply
  Std.WP.Variant.evalsBelow_ofMeasure_nat Std.WP.Variant.rel_ofMeasure_nat

/-- What a `decreasing` measure over `Int`s leaves to prove. -/
@[pverifySimp] theorem Int.toNat_lt_toNat_iff' (a b : Int) : a.toNat < b.toNat ↔ 0 < b ∧ a < b := by
  omega

/-! Handler states are destructured (`pv_destruct`), so equations between
them split into equations between their fields. -/
attribute [pverifySimp] HState.mk.injEq Log.mk.injEq MachineState.mk.injEq

/-- Quantifiers over packets range over their fields. -/
@[pverifySimp] theorem Packet.forall_iff (P : Packet E → Prop) :
    (∀ p, P p) ↔ ∀ src dst ev, P ⟨src, dst, ev⟩ :=
  ⟨fun h _ _ _ => h _, fun h p => h p.src p.dst p.ev⟩

/-! `foreach` cursors: the current item is a member of what is iterated. -/
@[pverifySimp] theorem PIterable.toList_list {T : Type} (l : List T) : PIterable.toList l = l := rfl
@[pverifySimp] theorem PIterable.toList_pset {T : Type} [DecidableEq T] (s : PSet T) :
    PIterable.toList s = s.support := rfl
@[pverifySimp] theorem PIterable.toList_pmap {K V : Type} [DecidableEq K] (m : PMap K V) :
    PIterable.toList m = m.keySet.support := rfl
attribute [pverifySimp] Std.Internal.ForIn.toList_list PSet.mem_support PMap.mem_keySet

open Lean Meta Elab Tactic in
/-- For each `foreach` cursor hypothesis `xs = pref ++ cur :: suff`, add
`cur ∈ xs`. -/
elab "pv_cursor_mem" : tactic => do
  let mut out := []
  for g in ← getGoals do
    let g' ← g.withContext do
      let mut g := g
      for d in ← getLCtx do
        if d.isImplementationDetail then continue
        let some (_, lhs, rhs) := (← instantiateMVars d.type).eq? | continue
        unless rhs.isAppOfArity ``HAppend.hAppend 6 do continue
        let tail := rhs.appArg!
        unless tail.isAppOfArity ``List.cons 3 do continue
        let cur := tail.appFn!.appArg!
        let memR ← mkFreshExprMVar (← mkAppM ``Membership.mem #[rhs, cur])
        let rest ← try Tactic.run memR.mvarId! (evalTactic (← `(tactic| simp)))
          catch _ => pure [memR.mvarId!]
        unless rest.isEmpty do continue
        let motive ← withLocalDeclD `x (← inferType lhs) fun x => do
          mkLambdaFVars #[x] (← mkAppM ``Membership.mem #[x, cur])
        let pf ← mkEqNDRec motive memR (← mkEqSymm (mkFVar d.fvarId))
        let (_, g2) ← (← g.assert `h_cur (← mkAppM ``Membership.mem #[lhs, cur]) pf).intro1P
        g := g2
      pure g
    out := out ++ [g']
  setGoals out

open Lean Meta Elab Tactic in
/-- Clear the let-bound locals nothing refers to any more (handler locals and
`do` join points, inlined by normalisation). -/
elab "pv_clear_lets" : tactic => liftMetaTactic fun g => do
  let lets := (← g.getDecl).lctx.foldl (init := #[]) fun acc d =>
    if d.isLet && !d.isImplementationDetail then acc.push d.fvarId else acc
  return [← g.tryClearMany lets]

open Lean Elab Tactic in
/-- Split every hypothesis that is (syntactically) a conjunction. -/
elab "pv_split_ands" : tactic => liftMetaTactic fun g => return [← g.casesAnd]

open Lean Meta Elab Tactic in
/-- Destructure hypotheses that are conjunctions or existentials, and locals of
a message type (`PLean.Msg`), a handler-state type (`HState`, `Log`,
`MachineState`) or a machine-wrapper type (a structure whose only field is
`ref`), so the obligation mentions their fields directly. -/
elab "pv_destruct" : tactic => liftMetaTactic fun g => do
  let env ← getEnv
  g.casesRec fun d => do
    if d.isImplementationDetail || d.isLet then return false
    let t ← instantiateMVars d.type
    if t.isAppOfArity ``And 2 || t.isAppOfArity ``Exists 2 then return true
    match t.getAppFn with
    | .const c _ =>
      if c == `PLean.Msg || c == ``PLean.HState || c == ``PLean.Log ||
          c == ``PLean.MachineState then return true
      match getStructureInfo? env c with
      | some info => return info.fieldNames == #[`ref]
      | none => return false
    | _ => return false

end PLean
