/-
PLean Phase-1 Task 10 — SMT round-trip smoke test.

Confirms that Loom's `loom_smt` tactic can find and run the bundled
solver binaries (z3/cvc5) from a PLean source file. This is a
forward-looking check: M1 closes via `grind` and needs no SMT, but
Phase-3's obligation generator will dispatch to SMT for triples that
`grind` can't close.

Risk R3 in PLAN_P1: the lakefile downloads the solvers into
`Loom/.lake/build/`, and Loom's `Loom/SMT.lean` resolves them via
`currentDirectory!` — confirm that resolution works from a PLean
source dir, not just from a Loom one.

This file is intentionally tiny: a single trivial first-order goal
that any solver should close in milliseconds. If the solver isn't
found, the error surfaces at compile time with a clear "binary not
found" message — which is enough information to plan a Phase-3 fix
(e.g., setting `loom.solver.smt.path` explicitly).
-/
import Loom.SMT
import Loom.MonadAlgebras.WP.Options

-- Default solver is `grind`; we explicitly switch to cvc5 to actually
-- exercise the SMT path.
set_option loom.solver "cvc5"

namespace PLean.SmtRoundtrip

/-- Trivial first-order goal: ∀ x : Nat, x = x. -/
example (x : Nat) : x = x := by
  loom_smt

end PLean.SmtRoundtrip
