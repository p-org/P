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

/-- A machine state taken apart, equated with one that is not: one equation per field.
The frame of a loop that runs before its handler assigns a field leaves
`{ state := s, fields := f, … } = g.machines dst`, where `MachineState.mk.injEq` does not
apply; the solvers cope with it, but their proofs then often do not replay. -/
@[pverifySimp] theorem MachineState.mk_eq_iff (x : MachineState S E F G W)
    (s : Option S) (f : F) (p : Option G) (w : Option W) (h : Bool) :
    (MachineState.mk s f p w h = x) =
      (s = x.state ∧ f = x.fields ∧ p = x.pending ∧ w = x.waiting ∧ h = x.halted) := by
  cases x; simp

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

/-! A `foreach` invariant may name the items already visited (`pref`): none at entry, one
more after each iteration. -/
attribute [pverifySimp] List.not_mem_nil List.mem_append List.mem_singleton

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

/-- An invariant `p`, named `name`, in an obligation's post-condition. `vcgen`
keeps it whole, so `pv_split_post` can tag the goal with the invariant. -/
def PostInv (_name : String) (p : Prop) : Prop := p

@[pverifySimp] theorem PostInv.eq (n : String) (p : Prop) : PostInv n p = p := rfl

/-- The tag `pv_split_post` gives a goal of invariant `inv`. -/
def invTag (inv : String) : Lean.Name := .str (.mkSimple "pv_inv") inv

/-- The invariant a goal tag (`invTag`, possibly extended by later tactics)
names. -/
def invOfTag (tag : Lean.Name) : Option String :=
  match tag.components with
  | .str .anonymous "pv_inv" :: .str .anonymous inv :: _ => some inv
  | _ => none

open Lean Meta in
/-- Split conjunctions, and for a goal `PostInv inv P` tag it `invTag inv`,
replace it by `P` and introduce `P`'s binders. -/
partial def splitPost (g : MVarId) : MetaM (List MVarId) := do
  let t := (← instantiateMVars (← g.getType)).consumeMData.headBeta
  if t.isAppOfArity ``And 2 then
    let gs ← g.apply (mkConst ``And.intro)
    return (← gs.mapM splitPost).flatten
  if t.isAppOfArity ``PostInv 2 then
    let .lit (.strVal inv) := t.appFn!.appArg!.consumeMData | return [g]
    let g' ← g.replaceTargetDefEq t.appArg!
    g'.setTag (invTag inv)
    return [(← g'.intros).2]
  return [g]

open Lean Elab Tactic in
/-- Split each goal by the invariants of its post-condition (`splitPost`). -/
elab "pv_split_post" : tactic => do
  setGoals ((← (← getGoals).mapM fun g => splitPost g).flatten)

open Lean Meta Elab Tactic in
/-- Clear the let-bound locals nothing refers to any more (handler locals and
`do` join points, inlined by normalisation). -/
elab "pv_clear_lets" : tactic => liftMetaTactic fun g => do
  let lets := (← g.getDecl).lctx.foldl (init := #[]) fun acc d =>
    if d.isLet && !d.isImplementationDetail then acc.push d.fvarId else acc
  return [← g.tryClearMany lets]

open Lean Meta Elab Tactic in
/-- For each update term in the goal or a hypothesis (`s.add x`, `s.del x`, `m.put k v`,
`m.del k`, the empty set and map), add the lookup's equation at that term as a hypothesis,
e.g. `∀ y, (s.add x).mem y = (decide (y = x) || s.mem y)`.

Normalisation computes a lookup where the update is the lookup's own argument. A container
reached through an equation (a set stored in a map, the payload of a message just sent) is
left to the solvers, to which `PSet.mem` and `PMap.get` are unknown functions. These facts
say what they are at the updates the goal has. They are instances of proved lemmas, and
they quantify over the element only: with the lookups' equations for every set (as
`crush_unfold` would send them) a solver no longer finds a model of a goal that does not
hold, and the report would lose its counterexample.

An update term under a binder is skipped. A term gets its fact only when the goal mentions
a lookup at its element (key) type, whose `DecidableEq` instance the fact then uses, so that
the solver sees one lookup function per type. -/
elab "pv_lookup_facts" : tactic => withMainContext do
  let g ← getMainGoal
  let mut es : Array Expr := #[← instantiateMVars (← g.getType)]
  for d in ← getLCtx do
    unless d.isImplementationDetail do es := es.push (← instantiateMVars d.type)
  -- the update terms, and the `DecidableEq` instance of the goal's lookups at each type
  let upd ← IO.mkRef (#[] : Array Expr)
  let insts ← IO.mkRef (#[] : Array (Expr × Expr))
  let noteInst (ty inst : Expr) : MetaM Unit := do
    unless ty.hasLooseBVars || inst.hasLooseBVars || (← insts.get).any (·.1 == ty) do
      insts.modify (·.push (ty, inst))
  for e in es do
    e.forEach' fun t => do
      if t.isAppOfArity ``PSet.mem 4 then noteInst (t.getArg! 0) (t.getArg! 1)
      else if t.isAppOfArity ``PMap.get 5 then noteInst (t.getArg! 0) (t.getArg! 2)
      else if !t.hasLooseBVars && (t.isAppOfArity ``PSet.add 3 || t.isAppOfArity ``PSet.del 3
          || t.isAppOfArity ``PSet.empty 1 || t.isAppOfArity ``PMap.put 5
          || t.isAppOfArity ``PMap.del 4 || t.isAppOfArity ``PMap.empty 2) then
        unless (← upd.get).contains t do upd.modify (·.push t)
      return true
  let insts ← insts.get
  let mut g := g
  for t in ← upd.get do
    let keyTy := t.getArg! 0
    let some (_, inst) := insts.find? (·.1 == keyTy) | continue
    let proof? ← (try
      if t.isAppOfArity ``PSet.add 3 then
        some <$> mkAppOptM ``PSet.mem_add #[keyTy, inst, t.getArg! 1, t.getArg! 2]
      else if t.isAppOfArity ``PSet.del 3 then
        some <$> mkAppOptM ``PSet.mem_del #[keyTy, inst, t.getArg! 1, t.getArg! 2]
      else if t.isAppOfArity ``PSet.empty 1 then
        some <$> mkAppOptM ``PSet.mem_empty #[keyTy, inst]
      else if t.isAppOfArity ``PMap.put 5 then
        some <$> withLocalDeclD `k keyTy fun k => do
          mkLambdaFVars #[k] (← mkAppOptM ``PMap.get_put
            #[keyTy, t.getArg! 1, inst, t.getArg! 2, t.getArg! 3, k, t.getArg! 4])
      else if t.isAppOfArity ``PMap.del 4 then
        some <$> mkAppOptM ``PMap.get_del #[keyTy, t.getArg! 1, inst, t.getArg! 2, t.getArg! 3]
      else
        some <$> mkAppOptM ``PMap.get_empty #[keyTy, t.getArg! 1, inst]
      catch _ => pure none)
    let some proof := proof? | continue
    let ty ← instantiateMVars (← inferType proof)
    let (_, g') ← (← g.assert `pv_lookup ty proof).intro1P
    g := g'
  replaceMainGoal [g]

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
