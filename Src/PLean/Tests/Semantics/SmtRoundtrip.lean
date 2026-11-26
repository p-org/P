/-
SMT round-trip smoke test.

Confirms that Loom's `loom_smt` tactic can find and run the bundled
solver binaries (z3/cvc5) from a PLean source file and that the solver
can discharge non-trivial first-order goals. Three probes:
  (1) binary reachability via the trivial `x = x` form,
  (2) a quantified linear-arithmetic claim, and
  (3) a `forall`/`exists` mix that would defeat `omega`.

The lakefile downloads the solvers into `Loom/.lake/build/`, and Loom's
`Loom/SMT.lean` resolves them via `currentDirectory!`; this test pins
that resolution from a PLean source dir.

## What this test does NOT cover

`loom_smt`'s translation backend (`lean-auto`) refuses higher-order
inputs. PLean's `GlobalState.sent : Label → Bool` is a record field
that's first-order in Lean but higher-order from SMT-LIB's
perspective. Goals like `(s.addSent lbl).sent lbl = true` therefore
fail to translate with "`lamSort2SSortAux :: Unexpected error. Higher
order input?`" — the obligation generator's prep chain
(`pverify_smt_prep`) defunctionalises before reaching `loom_smt` to
side-step this.
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
