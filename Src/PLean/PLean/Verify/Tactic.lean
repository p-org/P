/-
PLean.Verify.Tactic — atomic `pverify_*` tactics.

`#pverify M` is an SMT-discharge command; the tactics here are the
user-facing primitives for manual proofs of obligations SMT can't
close, registered via `@[pverifyProof]`. The pre-SMT simp set lives
in `Verify/SimpAttrs.lean` (lemmas + the `pverifySimp` attribute);
this file is purely tactic syntax.

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
tactics; neither inlines lemma names — bare references would get
hygiene marks at expansion.
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
import PLean.Verify.SimpLemmas

open Lean Elab Tactic Meta

namespace PLean

/-! ## Diagnostic refs

`#pverify`'s obligation generator wraps each obligation in
`pverify_log_failure_else_sorry`. When the inner chain fails, the
SMT solver's diagnostic (counter-example / `unknown` reason) and any
non-SMT tactic-error text are stashed in these refs before the chain
falls through to `sorry`. The obligation generator clears them before
each obligation and reads them after.

We need `IO.Ref`s rather than message-log entries because the surrounding
`first | … | …` ladder rolls back the log on rollback; the refs survive
tactic-state restoration. -/

initialize pverifySmtDiagRef : IO.Ref (Option String) ← IO.mkRef none
initialize pverifyTacDiagRef : IO.Ref (Option String) ← IO.mkRef none

/-- Run a tactic chain; on throw or unclosed goals, stash the
diagnostic in `pverifyTacDiagRef` and close with `sorry` so the
enclosing `theorem ... := by ...` still elaborates. The post-elab
`info.value.hasSorry` check tells the reporting layer which
obligations failed; the ref carries the WHY. -/
syntax "pverify_log_failure_else_sorry " tacticSeq : tactic
elab_rules : tactic
  | `(tactic| pverify_log_failure_else_sorry $ts:tacticSeq) =>
      withMainContext do
        let savedGoals ← getGoals
        let mut diagnostic : Option String := none
        try
          evalTactic (← `(tactic| ($ts:tacticSeq)))
        catch e =>
          diagnostic := some (← e.toMessageData.toString)
        if diagnostic.isNone && !(← getGoals).isEmpty then
          diagnostic := some
            "the `pverify` tactic chain did not close the goal"
        match diagnostic with
        | some msg =>
          setGoals savedGoals
          pverifyTacDiagRef.set (some msg)
          evalTactic (← `(tactic| sorry))
        | none => pure ()

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

/-- Pre-SMT normalisation: simp the `pverifySimp` set, destruct
state hypotheses, strip `WithName` wrappers, then unfold the
default-invariant predicates so lean-auto's monomorphizer sees applied
uninterpreted symbols and concrete `Label`/`Nat`/`Bool` atoms instead
of `Higher order input?`-flagged shapes.

Step ordering: `simp [pverifySimp]` must precede `sdestruct_state` so
`addSent` / `addReceived` / etc. expand into record literals while
the state still has its struct form; the subsequent destruct +
`dsimp only` iota-reduces `{ sent := f, ... }.sent l` to `f l`. -/
syntax "pverify_smt_prep" : tactic
macro_rules
  | `(tactic| pverify_smt_prep) => `(tactic| (
      try intros
      try simp only [pverifySimp] at *
      try sdestruct_state
      try unfold WithName at *
      try dsimp only at *
      try unfold PLean.DefaultInvariants at *
      try unfold PLean.UniqueActions at *
      try unfold PLean.IncreasingCount at *
      try unfold PLean.ReceivedSubsetSent at *
    ))

/-- SMT discharge: prep the goal, run `loom_smt`. On non-unsat the
solver throws; we stash the diagnostic in `pverifySmtDiagRef` and
re-throw so the surrounding `first |` can try fallbacks. -/
syntax "pverify_smt_close" : tactic
elab_rules : tactic
  | `(tactic| pverify_smt_close) =>
      withMainContext do
        try
          evalTactic (← `(tactic| (pverify_smt_prep; loom_smt [*])))
        catch e =>
          let msg ← e.toMessageData.toString
          pverifySmtDiagRef.set (some msg)
          throw e

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

/-- Sub-expression guard: fail unless the goal type mentions one of the
four default-invariant constants (`DefaultInvariants` / `UniqueActions`
/ `IncreasingCount` / `ReceivedSubsetSent`) somewhere in its tree.

`default_inv` runs `refine ⟨?_, ?_, ?_⟩` and per-conjunct rcases
tactics shaped for those four constants, so applying it to a goal that
contains *no* default-invariant content (e.g., a user invariant
`P1 ∧ P2 ∧ P3` of unrelated 3-arity) could mangle the goal. The guard
makes that fall-through cheap: `first | default_inv | …` rejects
unfit goals at entry. The check is permissive — the obligation
generator's post is `lemmaPred s ∧ DefaultInvariants s ∧ … `, so any
real obligation contains the constants. Pure user-invariant goals do
not. -/
elab_rules : tactic
  | `(tactic| default_inv_guard) => withMainContext do
      let goal ← getMainTarget
      let allowed : List Name :=
        [``PLean.DefaultInvariants, ``PLean.UniqueActions,
         ``PLean.IncreasingCount, ``PLean.ReceivedSubsetSent]
      let mentions := goal.find? fun e =>
        match e.getAppFn.constName? with
        | some n => allowed.contains n
        | none   => false
      if mentions.isNone then
        throwError "default_inv: this goal does not mention any of \
                    `DefaultInvariants` / `UniqueActions` / \
                    `IncreasingCount` / `ReceivedSubsetSent`; tactic \
                    declined to fire (would over-split a generic \
                    n-conjunct goal)"

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

/-- Shared closing chain for `pverify` / `pverify_default`. Tries
`default_inv` first (cheap when the goal head matches one of the four
default-invariant constants — guarded otherwise), then SMT, then the
arithmetic / boolean fallback. -/
syntax "pverify_close_chain" : tactic
macro_rules
  | `(tactic| pverify_close_chain) => `(tactic| (
      first
        | default_inv
        | pverify_smt_close
        | pverify_grind))

/-- Inverse-ordered close (SMT first), used by `pverify_default` whose
goal arrives with explicit `DefaultInvariants` content the SMT path
handles directly without needing `default_inv`'s case split. -/
syntax "pverify_close_chain_smt_first" : tactic
macro_rules
  | `(tactic| pverify_close_chain_smt_first) => `(tactic| (
      first
        | pverify_smt_close
        | default_inv
        | pverify_grind))

/-- The headline auto-discharge tactic.

`pverify_step_wp` already runs `wpgen` (which itself opens the triple
via `wpgen_intro`); calling `pverify_open_triple` here would re-try
`WPGen.intro` past the triple boundary and fail. `pverify_open_triple`
remains exposed standalone for users who want full control over the
manual proof's structure. -/
macro_rules
  | `(tactic| pverify) => `(tactic| (
      first
        | -- Post-equals-pre branch: the post-state predicate is
          -- structurally equal to one of the introduced pre-state
          -- hypotheses, so `assumption` closes the goal directly. This
          -- subsumes the trivial `pure ()` handler case but does NOT
          -- require the body to be `pure ()` — any handler whose post
          -- happens to match a pre-clause hits this branch first.
          (pverify_step_wp
           intros
           assumption)
        | (pverify_step_wp
           intros
           split_conjunction_hyps
           pverify_close_chain)
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
           pverify_close_chain_smt_first)
    ))

end PLean
