/-
PLean Phase-1 Task 10 — SMT round-trip smoke test.

Confirms that Loom's `loom_smt` tactic can find and run the bundled
solver binaries (z3/cvc5) from a PLean source file *and* that the
solver can discharge non-trivial first-order goals. This is a
forward-looking check: M1/M2 close via `grind` and need no SMT, but
Phase-3's obligation generator will dispatch to SMT for triples that
`grind` can't close (D22 step 4, currently deferred under REVIEW_P3
§2.5).

The earlier version of this file proved only `∀ x : Nat, x = x`,
which was a useful "binary reachable + spawning works" smoke test
but didn't exercise any reasoning. The current test additionally:
  (1) confirms binary reachability with the trivial `x = x` form,
  (2) discharges a quantified linear-arithmetic claim, and
  (3) handles a `forall`/`exists` mix that would defeat `omega`.

Risk R3 in PLAN_P1: the lakefile downloads the solvers into
`Loom/.lake/build/`, and Loom's `Loom/SMT.lean` resolves them via
`currentDirectory!` — confirm that resolution works from a PLean
source dir, not just from a Loom one. If the solver isn't found,
the error surfaces at compile time with a clear "binary not found"
message — enough information to plan a Phase-3 fix.

## What this test does NOT cover

`loom_smt`'s translation backend (`lean-auto`) refuses higher-order
inputs. PLean's `GlobalState.sent : Label → Bool` is a record field
that's *first-order in Lean* but *higher-order from SMT-LIB's
perspective* (a function in the goal universe). Goals like
`(s.addSent lbl).sent lbl = true` therefore fail to translate with
"`lamSort2SSortAux :: Unexpected error. Higher order input?`" even
after `unfold GlobalState.addSent`. Phase 3's eventual `loom_smt`
fallback in `pverify_solve` (REVIEW_P3 §2.5) will need a
defunctionalisation pre-pass — or to keep restricting SMT to
goals over `Nat`/`Int`/`Bool`/inductives only — before it can
discharge GlobalState-shaped triples. Tracked, not yet built.
-/
import Loom.SMT
import Loom.MonadAlgebras.WP.Options

-- Default Loom solver is `grind`; we explicitly switch to cvc5 so
-- every `loom_smt` invocation in this file actually spawns a solver
-- process.
set_option loom.solver "cvc5"

namespace PLean.SmtRoundtrip

/-! ## Goal 1: solver-binary reachability

The classic `x = x` form. The only purpose is to confirm the solver
process spawned and returned `unsat` to the negation of `True`.
Survives by the `trust_smt` axiom path. -/

example (x : Nat) : x = x := by
  loom_smt

/-! ## Goal 2: quantified linear arithmetic

Universally-quantified commutativity. `omega` could close this on
its own, but `omega` doesn't run unless we ask for it; the point of
this case is that the solver receives a real query and reasons
through the `∀` binder. -/

example : ∀ a b : Nat, a + b = b + a := by
  loom_smt

/-! ## Goal 3: existential elimination over a `Nat` predicate

A quantifier-mix: from `∃ a, P a ∧ a < 10` derive `∃ a, P a ∧ a < 100`.
`omega` doesn't handle existentials over an abstract predicate;
this forces actual SMT reasoning over a uninterpreted `P`. -/

example (P : Nat → Prop)
    (_h : ∃ a, P a ∧ a < 10) :
    ∃ a, P a ∧ a < 100 := by
  loom_smt

end PLean.SmtRoundtrip
