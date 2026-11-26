/-
Regression tests for the manual-proof helpers in `Verify/Tactic.lean`.
See `Verify/Tactic.lean`'s "Manual-proof helpers" section for the full
Hoare-triple shape table.

- `pverify_split_smt` — split top-level `∧` then SMT-discharge each;
- `pverify_carry_after_recv h` — discharge a `¬ inflight e (post)` /
  `inflight e (post) → P` clause across a step that only marks the
  dispatched label received.

`pverify_not_inflight`, `pverify_inflight_by`,
`pverify_machine_has_type`, and `pverify_not_inflight_by` exercise
the full dispatcher contract (`is_<ev>`, `Sig.Label`, the
`GlobalState` shape + a machine wrapper struct), so they are pinned
via the LockServer / RingLeader manual proofs rather than synthetic
miniatures here.

The tests use synthetic miniatures of the LockServer step shape so
they do NOT depend on the full M3 ports.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 8

namespace PLean.Tests.ManualProofHelpers

/-! ## `pverify_split_smt` — split a conjunction then SMT-discharge each.

Each conjunct goes to SMT independently after the outer `∧` is split. -/
example (a b : Nat) (_hab : a = b) : a = b ∧ b = a := by
  pverify_split_smt

example (a b c : Nat) (_hab : a = b) (_hbc : b = c) :
    a = b ∧ b = c ∧ a = c := by
  pverify_split_smt

/-! ## `pverify_carry_after_recv` — received-monotone shape.

The lemma synthesises the canonical "step marks `lbl` received" goal
shape: `¬ inflight e (post)` where `post.sent = pre.sent` and
`post.received e = decide(e = lbl) || pre.received e`. The pre-state
hypothesis says `e` was either already delivered or not in the buffer.

We mirror the shape from `PLean.GlobalState` directly to avoid
constructing a full `#gen_module` pmodule for the regression. -/

example (S : Type) [DecidableEq S] (sent received : S → Bool) (lbl e : S)
    (hPre : ¬ (sent e = true ∧ received e = false)) :
    ¬ (sent e = true ∧ (decide (e = lbl) || received e) = false) := by
  pverify_carry_after_recv hPre

/-- Same shape but the pre-state hypothesis is written as `(A ∧ B) → C`
(uncurried implication) rather than `¬(A ∧ B)`. Both reduce to the
same curried form `A → B → C`, but `simp only` matches them via
different lemmas (`and_imp` vs `not_and`), so the helper applies both. -/
example (S : Type) [DecidableEq S] (sent received : S → Bool) (lbl e : S)
    (hPre : sent e = true ∧ received e = false → False) :
    ¬ (sent e = true ∧ (decide (e = lbl) || received e) = false) := by
  pverify_carry_after_recv hPre

end PLean.Tests.ManualProofHelpers
