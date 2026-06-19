/-
Regression tests for the manual-proof helpers in `Verify/Tactic.lean`:

- `pverify_split_smt_close` — split top-level `∧` then SMT-close each;
- `pverify_unchanged` — discharge a clause unchanged by the step;
- `pverify_recv_only` — discharge a `¬ inflight e (post)` clause
  when the step only marks the dispatched label received.

These pin the behaviour the LockServer manual proofs rely on. They use
synthetic mini-pmodules so the test does NOT depend on the full M3 ports.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 8

namespace PLean.Tests.ManualProofHelpers

/-! ## `pverify_split_smt_close` — split a conjunction then SMT-close each.

Each conjunct goes to SMT independently after the outer `∧` is split. -/
example (a b : Nat) (_hab : a = b) : a = b ∧ b = a := by
  pverify_split_smt_close

example (a b c : Nat) (_hab : a = b) (_hbc : b = c) :
    a = b ∧ b = c ∧ a = c := by
  pverify_split_smt_close

/-! ## `pverify_unchanged` — close by assumption / solve_by_elim. -/

example (p : Prop) (hp : p) : p := by
  pverify_unchanged

example (p q : Prop) (h : p → q) (hp : p) : q := by
  pverify_unchanged

/-! ## `pverify_recv_only` — received-monotone shape.

The lemma synthesises the canonical "step marks `lbl` received" goal
shape: `¬ inflight e (post)` where `post.sent = pre.sent` and
`post.received e = decide(e = lbl) || pre.received e`. The pre-state
hypothesis says `e` was either already delivered or not in the buffer.

We mirror the shape from `PLean.GlobalState` directly to avoid
constructing a full `#gen_module` pmodule for the regression. -/

example (S : Type) [DecidableEq S] (sent received : S → Bool) (lbl e : S)
    (hPre : ¬ (sent e = true ∧ received e = false)) :
    ¬ (sent e = true ∧ (decide (e = lbl) || received e) = false) := by
  pverify_recv_only hPre

/-- Same shape but the pre-state hypothesis is written as `(A ∧ B) → C`
(uncurried implication) rather than `¬(A ∧ B)`. Both reduce to the
same curried form `A → B → C`, but `simp only` matches them via
different lemmas (`and_imp` vs `not_and`), so the helper applies both. -/
example (S : Type) [DecidableEq S] (sent received : S → Bool) (lbl e : S)
    (hPre : sent e = true ∧ received e = false → False) :
    ¬ (sent e = true ∧ (decide (e = lbl) || received e) = false) := by
  pverify_recv_only hPre

end PLean.Tests.ManualProofHelpers
