/-
PLean.Verify.Tactic — atomic `pverify_*` tactics.

`#pverify M` is an SMT-discharge command; the tactics here are the
user-facing primitives for manual proofs of obligations SMT can't
close, registered via `@[pverifyProof]`.

Composing a manual proof:

```lean
@[pverifyProof] theorem MyMod.M.S.<ev>_correct_safety
    (this : M) (param : payload) :
    triple ... := by
  pverify_open_triple
  pverify_step_wp
  intro s hpre
  obtain ⟨hLemma, hUA, hIC, hRS, lbl, hInflight, hTarget, hStateOf, hAction⟩ := hpre
  pverify_smt_close
```

Macro-hygiene rule: every simp lemma name lives inside a named tactic
helper. The obligation generator and manual proofs both call the named
tactics; neither inlines lemma names. This avoids hygiene marks turning
bare `simp [wp_bind]` into `simp [wp_bind✝]` at expansion.
-/
import Lean
import Loom.MonadAlgebras.WP.Basic
import Loom.MonadAlgebras.WP.Tactic
import Loom.SMT
import Mathlib.Tactic.Tauto
import PLean.Semantics.Default
import PLean.Semantics.GlobalState
import PLean.Semantics.Predicates
import PLean.Semantics.Primitives
import PLean.Verify.SimpAttrs

open Lean Elab Tactic Meta

/-! ## `pverifySimp` — pre-SMT simplification set

Tagged lemmas turn higher-order constructs (function-typed record
fields, function/iff/tuple equalities) into first-order forms
lean-auto can translate. The attribute is registered in
`Verify/SimpAttrs.lean` so it's available before any `@[pverifySimp]`
use site here is elaborated. -/

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

namespace PLean

/-- Peel a `triple pre body post` goal: replaces it with
`pre ≤ wpg.get post` for a synthesised `wpg : WPGen body`. Alias for
Loom's `wpgen_intro`. -/
syntax "pverify_open_triple" : tactic
macro_rules
  | `(tactic| pverify_open_triple) => `(tactic| (apply WPGen.intro; rotate_right))

/-- Run `wpgen` and clean up the post-`wpgen` plumbing.

After `wpgen`, the goal carries `WPGen.bind` / `WPGen.pure` shapes,
`GlobalState` update terms, and (for conditional handlers) Loom's
`WithName` / `iInf` machinery from `if_pos` / `if_neg` branches. We
strip each in sequence so the goal arrives at SMT prep as a clean
propositional fact. -/
syntax "pverify_step_wp" : tactic
macro_rules
  | `(tactic| pverify_step_wp) => `(tactic| (
      wpgen <;> first | apply WPGen.default | skip
      try simp only [loomLogicSimp, loomWpSimp, loomWPGenRewrite]
      try simp only [PLean.GlobalState.addSent, PLean.GlobalState.bumpActionCount,
                     PLean.GlobalState.addReceived, PLean.GlobalState.updateMachine]
      try simp only [WithName.mk', WithName.erase, typeWithName.erase]
      try simp only [iInf_apply, iInf_Prop_eq, iSup_apply, iSup_Prop_eq]
      try simp only [if_true, if_false]
    ))

/-- Unfold framework-level state predicates so subsequent simp / grind
/ SMT calls see boolean / arithmetic content. User invariants and
lemma bundles are unfolded by the obligation generator's preamble. -/
syntax "pverify_unfold" : tactic
macro_rules
  | `(tactic| pverify_unfold) => `(tactic| (
      try unfold PLean.DefaultInvariants PLean.UniqueActions
                 PLean.IncreasingCount PLean.ReceivedSubsetSent
                 PLean.inflight PLean.sent PLean.received
                 PLean.precedes PLean.Label.targets? PLean.stateOf
    ))

/-- Aggressive state-update simp at the propositional VC level (after
`intro s hpre`). Same simp set as `pverify_step_wp` but applied at
`*`. -/
syntax "pverify_normalize_state" : tactic
macro_rules
  | `(tactic| pverify_normalize_state) => `(tactic| (
      try simp only [PLean.GlobalState.addSent, PLean.GlobalState.bumpActionCount,
                     PLean.GlobalState.addReceived, PLean.GlobalState.updateMachine] at *
    ))

/-- Destruct every `GlobalState`-typed local into its four fields,
making them top-level uninterpreted symbols rather than struct
projections. Without this, lean-auto rejects with "Higher order input?"
on the first `s.sent l` projection. -/
syntax "sdestruct_state" : tactic
elab_rules : tactic
  | `(tactic| sdestruct_state) => withMainContext do
    let lctx ← getLCtx
    for ldecl in lctx do
      if ldecl.isImplementationDetail then continue
      -- Whnf so abbreviations like `GS = GlobalState Sig` reduce to the head.
      let ty ← Lean.Meta.whnf (← Lean.Meta.inferType ldecl.toExpr)
      if ty.consumeMData.isAppOfArity ``PLean.GlobalState 1 then
        let hName := ldecl.userName
        let stx ← `(tactic|
          obtain ⟨_, _, _, _⟩ := $(mkIdent hName))
        try evalTactic stx
        catch _ => pure ()

/-- Veil-style pre-SMT normalisation. After this runs, the goal is
over applied uninterpreted symbols and concrete `Label`/`Nat`/`Bool`
atoms — the shape lean-auto's monomorphizer translates without hitting
"Higher order input?".

Step ordering matters: `simp [pverifySimp]` must run BEFORE
`sdestruct_state` so `addSent` / `addReceived` / etc. expand into
record literals while the state still has its struct form; the
subsequent destruct + `dsimp only` then iota-reduces
`{ sent := f, ... }.sent l` to `f l`. -/
syntax "pverify_smt_prep" : tactic
macro_rules
  | `(tactic| pverify_smt_prep) => `(tactic| (
      try intros
      try simp only [pverifySimp] at *
      try sdestruct_state
      -- WithName n α = α; unfolding it strips Loom's named-assertion
      -- wrappers that survive `pverify_step_wp` in some elaboration
      -- contexts (Veil's recipe doesn't need this; their DSL doesn't go
      -- through Loom's WPGen).
      try unfold WithName at *
      try dsimp only at *
      -- Each `unfold` is its own `try` because `unfold X at *` fails
      -- atomically when `X` doesn't appear anywhere — we want each
      -- to fire independently.
      try unfold PLean.DefaultInvariants at *
      try unfold PLean.UniqueActions at *
      try unfold PLean.IncreasingCount at *
      try unfold PLean.ReceivedSubsetSent at *
    ))

/-- SMT discharge: prep the goal then send to cvc5/z3 via Loom's
`loom_smt`. On `unsat` the goal closes via `trust_smt`; on
`sat`/`unknown`/translation failure the call throws. -/
syntax "pverify_smt_close" : tactic
macro_rules
  | `(tactic| pverify_smt_close) => `(tactic| (
      pverify_smt_prep
      loom_smt [*]
    ))

/-- Arithmetic / boolean fallback for default-invariant goals SMT
can't translate (e.g., when the goal involves `GlobalState`'s
function-typed fields in a shape that defeats `funextEq`). -/
syntax "pverify_grind" : tactic
macro_rules
  | `(tactic| pverify_grind) => `(tactic| (
      try intros
      first | grind | (refine ⟨?_, ?_⟩ <;> grind) | omega | tauto | assumption
    ))

/-! ## `default_inv` — `DefaultInvariants` discharge

The proof shape for `UniqueActions` / `IncreasingCount` /
`ReceivedSubsetSent` preservation is mechanical and depends only on
the handler's primitive footprint. The chain below splits the 3-way
conjunction, intros the per-conjunct labels, simps `addSent`-shaped
post-state reads to a disjunction, `rcases`-splits new vs old, and
closes each leaf via `solve_by_elim` / `Nat.lt_irrefl` /
`Nat.lt_succ_*` / `grind` / `omega`. The auto-discharge path doesn't
rely on user-named hypotheses — `solve_by_elim` finds the pre-state
hypothesis by type. -/
syntax "default_inv_guard" : tactic
syntax "default_inv" : tactic

/-- No-op placeholder — kept as a hook for a future head-symbol guard
if `default_inv` over-fires on some user invariant. Current call sites
all wrap in `first | default_inv | ...` so fall-through is harmless. -/
elab_rules : tactic
  | `(tactic| default_inv_guard) => pure ()

macro_rules
  | `(tactic| default_inv) => `(tactic| (
      default_inv_guard
      -- Goal might be `(WPGen.default x).get post s` or already
      -- `DefaultInvariants s'`; reduce both via `WPGen.default` +
      -- the WP rewrite set so we land at `DefaultInvariants <update>`.
      try simp [WPGen.default, loomWPGenRewrite, loomLogicLiftSimp]
      -- Unfold `DefaultInvariants` AND its three components in the
      -- local context so pre-state hypotheses are in implication form
      -- (otherwise `grind` can't peel the named constants).
      try unfold PLean.DefaultInvariants
      try simp only [PLean.UniqueActions, PLean.IncreasingCount,
                     PLean.ReceivedSubsetSent] at *
      try refine ⟨?_, ?_, ?_⟩
      all_goals first
        | (intro a b hne ha hb
           simp at ha hb
           rcases ha with rfl | hAprev <;>
             rcases hb with rfl | hBprev <;>
             first
               | (exact (hne rfl).elim)
               | (intro hEq
                  exfalso
                  -- One label is new (count = s.actionCount), the
                  -- other old (count < s.actionCount by IC). solve_by_elim
                  -- finds IC; Nat.lt_irrefl closes.
                  solve_by_elim [Nat.lt_irrefl])
               | -- Both old labels: pre-state UA closes.
                 (solve_by_elim)
               | grind
               | omega)
        | (intro a ha
           simp at ha
           rcases ha with rfl | hPrev
           · first | exact Nat.lt_succ_self _ | omega | grind
           · first | (apply Nat.lt_succ_of_lt; first | grind | omega)
                    | omega | grind)
        | (intro a ha
           simp at ha
           simp
           first
             | (rcases ha with rfl | hPrev
                · -- new received = inflight lbl; conclusion: it was sent.
                  first | (right; grind) | grind | tauto
                · -- old received: pre-state RS suffices.
                  first | (right; grind) | grind | tauto)
             | first | grind | tauto | assumption)
        | -- No-send / no-receive fallback: pre-state's UA/IC/RS holds.
          (first | grind | tauto | assumption | omega)
    ))

/-- Walk the local context and `obtain`-split every `(A ∧ B)` /
`(A ∧ B ∧ C)` hypothesis. After `pverify_step_wp` + `intros` the
precondition lives as a folded conjunction in one anonymous hypothesis;
flattening lets `solve_by_elim` / `assumption` find each clause by
type.

The 16-iteration cap is a safety bound, not a real limit (any real
obligation flattens in a handful of steps). Implemented as `elab_rules`
so it can use `Lean.Meta.inferType` to inspect hypothesis types. -/
syntax "split_conjunction_hyps" : tactic
elab_rules : tactic
  | `(tactic| split_conjunction_hyps) => withMainContext do
    let mut keepGoing := true
    let mut iter := 0
    while keepGoing && iter < 16 do
      iter := iter + 1
      keepGoing := false
      let lctx ← getLCtx
      for ldecl in lctx do
        if ldecl.isImplementationDetail then continue
        let ty ← Lean.Meta.inferType ldecl.toExpr
        if ty.consumeMData.isAppOfArity ``And 2 then
          let hName := ldecl.userName
          let stx ← `(tactic| obtain ⟨_, _⟩ := $(mkIdent hName))
          try
            evalTactic stx
            keepGoing := true
            break
          catch _ =>
            continue

syntax (name := pverifyTactic) "pverify" : tactic
syntax (name := pverifyDefaultTactic) "pverify_default" : tactic

/-- The headline auto-discharge tactic.

`pverify_step_wp` already runs `wpgen` (which itself opens the triple
via `wpgen_intro`); calling `pverify_open_triple` here would re-try
`WPGen.intro` past the triple boundary and fail. `pverify_open_triple`
remains exposed standalone for users who want full control over the
manual proof's structure. -/
macro_rules
  | `(tactic| pverify) => `(tactic| (
      first
        | -- Trivial `pure ()` handler: post-state predicate equals one
          -- of the introduced pre-state hypotheses; `assumption` finds it.
          (pverify_step_wp
           intros
           assumption)
        | (pverify_step_wp
           intros
           split_conjunction_hyps
           first
             | default_inv
             | pverify_smt_close
             | pverify_grind)
    ))

macro_rules
  | `(tactic| pverify_default) => `(tactic| (
      pverify_step_wp
      first
        | (intro s h; exact h)
        | (intros
           -- Unfold so the precondition's `DefaultInvariants s` becomes
           -- `(∀ a b, ...) ∧ (∀ a, ...) ∧ (∀ a, ...)` and can be split.
           simp only [PLean.DefaultInvariants, PLean.UniqueActions,
                      PLean.IncreasingCount, PLean.ReceivedSubsetSent] at *
           split_conjunction_hyps
           first
             | pverify_smt_close
             | default_inv
             | pverify_grind)
    ))

end PLean
