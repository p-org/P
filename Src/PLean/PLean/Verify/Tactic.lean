/-
PLean.Verify.Tactic — `pverify` and `default_inv` tactics (D22, D28).

`pverify` is PLean's `loom_solve`-equivalent: a single tactic that
unfolds handler bodies + invariants, runs `wpgen`, splits the
post-conjunction, and routes each VC through `default_inv` (for the
three sanity invariants) or `grind` / SMT (for everything else).

The implementation reuses the M1/M2 manual proof shape line-for-line:
  unfold <handler> <inv-aliases> ...
  wpgen <;> first | apply WPGen.default | skip
  intro s <hyp pattern>
  refine <conj split>
  (per VC) ... default_inv | grind | loom_smt

Decision D3 (PLAN_P1): we don't depend on `CaseStudies.Tactic.loom_solve`
(it requires Cashmere's `WithName` registration, which `#gen_module`
doesn't produce — confirmed empirically end of Phase 2). Instead we
recompose the underlying primitives directly.

## How `pverify` is invoked

The Phase-3 obligation generator emits

  theorem M.S.<ev>_correct ... : triple ... := by pverify

and `pverify` is responsible for closing every triple it sees. Users
can also call it explicitly (e.g., to refactor M2's hand-written
triples into one-liners).

## Variants (Phase-3 MVP)

- `pverify` — standard. Default-invariant VCs route through
  `default_inv`; everything else falls back to `pverify_solve`.
- `pverify_default` — same shape as `pverify`, used by the
  obligation generator for `prove default;` directives. The
  obligation generator's preamble takes care of unfolding `using`
  lemma bundles before either is invoked, so we don't yet expose a
  `pverify using …` tactic-level surface (PLAN_P3 D22 reserves
  `pverify using` / `pverify!` / `pverify?` as future variants).
-/
import Lean
import Loom.MonadAlgebras.WP.Basic
import Loom.MonadAlgebras.WP.Tactic
import Mathlib.Tactic.Tauto
import PLean.Semantics.Default
import PLean.Semantics.GlobalState
import PLean.Semantics.Predicates
import PLean.Semantics.Primitives

open Lean Elab Tactic Meta

namespace PLean

/-! ## `default_inv` — discharge a default-invariant goal (D28).

The three default invariants (`UniqueActions`, `IncreasingCount`,
`ReceivedSubsetSent`) are preserved by every well-formed handler by
an essentially mechanical argument that depends only on which
primitives the handler called. PVerifier emits a similar mechanical
discharger.

The shape is fixed: `intro <a> [<b>]?; intro <hpre>...; simp only
[GlobalState.addSent, GlobalState.addReceived,
GlobalState.bumpActionCount, GlobalState.updateMachine] at *; rcases
<hpre> with rfl | hPrev; <per-case>`.

PLAN_P3 D28 step 3 mandates that `default_inv` only fire on goals
whose head symbol is `DefaultInvariants` / `UniqueActions` /
`IncreasingCount` / `ReceivedSubsetSent`. The `default_inv_guard`
tactic (below, REVIEW_P3 §1) inspects the goal head; on mismatch it
fails so a `first | default_inv | ...` chain falls through cleanly
instead of mis-splitting a coincidentally-3-conjunct user invariant. -/

syntax "default_inv_guard" : tactic
syntax "default_inv" : tactic

/-- Goal-head guard for `default_inv` (REVIEW_P3 §1).

Succeeds iff the current goal's head symbol (after stripping
`∀`/`→` binders that arise from a not-yet-`intro`'d goal) is one of
the four default-invariant constants. Fails otherwise so the caller
can fall through. -/
elab_rules : tactic
  | `(tactic| default_inv_guard) => withMainContext do
    let goal ← getMainTarget
    -- The goal at the head of `default_inv`'s call sites is always a
    -- propositional fact (after `wpgen` + `try intros`); strip metadata
    -- and look at the application head.
    let head := goal.consumeMData |>.getAppFn
    let allowedHeadsMsg :=
      "applies only to goals headed by `DefaultInvariants`, " ++
      "`UniqueActions`, `IncreasingCount`, or `ReceivedSubsetSent`"
    let some headName := head.constName? |
      throwError "default_inv: {allowedHeadsMsg} (goal head is not a constant)"
    let allowed : List Name :=
      [``PLean.DefaultInvariants, ``PLean.UniqueActions,
       ``PLean.IncreasingCount, ``PLean.ReceivedSubsetSent]
    unless allowed.contains headName do
      throwError "default_inv: {allowedHeadsMsg}"

macro_rules
  | `(tactic| default_inv) => `(tactic| (
      -- Step 1: refuse to fire unless the goal head is genuinely a
      -- default-invariant constant. `pverify` chains `(first |
      -- default_inv | pverify_solve)`, so on mismatch we want a clean
      -- `fail` that doesn't leave the goal partially split.
      default_inv_guard
      -- Step 2: reduce the goal to its three component conjuncts.
      try unfold PLean.DefaultInvariants
      try unfold PLean.UniqueActions PLean.IncreasingCount PLean.ReceivedSubsetSent
      -- For `DefaultInvariants` (a 3-tuple ∧), split.
      first
        | (refine ⟨?_, ?_, ?_⟩
           all_goals (
             intro a ha
             try (intro b hne hb;
                  simp only [PLean.GlobalState.addSent,
                             PLean.GlobalState.bumpActionCount,
                             PLean.GlobalState.addReceived,
                             PLean.GlobalState.updateMachine]
                    at ha hb ⊢ <;>
                    omega)
             try (simp only [PLean.GlobalState.addSent,
                             PLean.GlobalState.bumpActionCount,
                             PLean.GlobalState.addReceived,
                             PLean.GlobalState.updateMachine]
                    at ha ⊢ <;>
                    omega)))
        | (intro a ha
           simp only [PLean.GlobalState.addSent,
                      PLean.GlobalState.bumpActionCount,
                      PLean.GlobalState.addReceived,
                      PLean.GlobalState.updateMachine]
             at ha ⊢ <;>
           omega)
    ))

/-! ## `pverify` — the headline P-verification tactic (D22).

Pipeline:
  1. Unfold the handler and its referenced predicates / invariants.
  2. `wpgen <;> first | apply WPGen.default | skip` to step through
     the body.
  3. Introduce the precondition hypothesis and split the post-state
     conjunction.
  4. Per VC, try `default_inv` first (for sanity invariants) then
     `grind` then `simp`-only fallback. Failures show up as residual
     goals.

Implementation note: the user invariant bodies typically contain
`forall`/`exists` over `MachineRef`, plus `inflight`/`sent`/`is_<ev>`/
`<M>_allocated` predicates. We unfold those eagerly so `grind` sees
boolean / arithmetic content.
-/

syntax (name := pverifyTactic) "pverify" : tactic
syntax (name := pverifyDefaultTactic) "pverify_default" : tactic

/-- `pverify_unfold` — Step 1 of D22. Unfolds default invariants and
state predicates so subsequent `grind` / SMT calls see boolean /
arithmetic atoms. Leaves user invariants opaque (they're typically
pulled in by the explicit `unfold` line in the obligation generator
preamble). -/
syntax "pverify_unfold" : tactic
macro_rules
  | `(tactic| pverify_unfold) => `(tactic| (
      try unfold PLean.DefaultInvariants PLean.UniqueActions
                 PLean.IncreasingCount PLean.ReceivedSubsetSent
                 PLean.inflight PLean.sent PLean.received
                 PLean.precedes PLean.Label.targets? PLean.stateOf
    ))

/-- `pverify_wp` — Step 2 of D22. Steps through the handler body via
`wpgen` plus the `WPGen.default` fallback, then simps through the
WPGen wrappers and post-state computation so each VC is over a
concrete `GlobalState` update term. -/
syntax "pverify_wp" : tactic
macro_rules
  | `(tactic| pverify_wp) => `(tactic| (
      wpgen <;> first | apply WPGen.default | skip
      try simp only [WPGen.pure, WPGen.bind, WPGen.map, WPGen.if]
      try simp only [PLean.GlobalState.addSent, PLean.GlobalState.bumpActionCount,
                     PLean.GlobalState.addReceived, PLean.GlobalState.updateMachine]
    ))

/-- `pverify_solve` — Step 4 of D22. Closes a single VC via
`grind` / And-split + `grind` / `omega` / `tauto`.

Fallback ordering (REVIEW_P3 §4.3): `assumption` → `grind` →
And-specific `refine ⟨?_, ?_⟩ <;> grind` → `omega` → `tauto`.

`tauto` is imported from `Mathlib.Tactic.Tauto` (Mathlib is already a
transitive dependency via Loom/lean-auto, but we make the import
explicit so a future Loom dep refactor doesn't silently break us).
The And-specific `refine ⟨?_, ?_⟩ <;> grind` replaces a prior blind
`(constructor <;> grind)` that would mis-pick on `Or` or
`Decidable`-headed goals. -/
syntax "pverify_solve" : tactic
macro_rules
  | `(tactic| pverify_solve) => `(tactic| (
      first
        | assumption
        | (try intros
           first | grind | (refine ⟨?_, ?_⟩ <;> grind) | omega | tauto)
    ))

/-- The default-only variant — used by `prove default;`. Tries
`default_inv` (D28) before the generic `pverify_solve` chain because
`prove default` obligations always end in a `DefaultInvariants`-shape
post-condition. Failures are surfaced as `unsolved goals` errors
and handled by the obligation generator's R19 logic, which captures
them as failure-report entries rather than letting them abort the
build. -/
macro_rules
  | `(tactic| pverify_default) => `(tactic| (
      pverify_unfold
      pverify_wp
      first
        | (intro s h; exact h)
        | (try intros; first | default_inv | pverify_solve)
    ))

/-- Standard `pverify`. Composition of the four phases above. The
first branch handles trivial `pure ()` handlers; the second tries
`default_inv` (now head-symbol-gated, REVIEW_P3 §1) and falls
through to `pverify_solve`'s `grind` chain.

`default_inv`'s `default_inv_guard` first step refuses to fire
unless the goal head is a default-invariant constant, so the
over-fire risk that motivated *not* wiring it into `pverify` in
the first review pass is gone — a 3-conjunct user invariant
trips the guard and falls cleanly to `pverify_solve`.

Phase-3 ships with incomplete automation per PLAN_P3 R15; failures
surface as `unsolved goals` errors that the obligation generator
captures and reports, leaving the build green for cases that *do*
discharge. -/
macro_rules
  | `(tactic| pverify) => `(tactic| (
      pverify_unfold
      pverify_wp
      first
        | (intro s h; exact h)
        | (try intros; first | default_inv | pverify_solve)
    ))

end PLean
