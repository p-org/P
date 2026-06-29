/-
Soundness regressions for state-implicit invariants:

  1. A genuinely-false invariant must FAIL to verify. (Pins that the
     bundle predicate `fun s => name s ∧ …` actually applies `s`,
     rather than ignoring it.)

  2. Invariants are emitted as `GS → Prop` (not closed `Prop`), so
     bare state references inside the body resolve to the pmodule's
     `system`-declared state binder.

  3. An inner `∀ s : GlobalState Sig, …` inside an invariant under a
     `system <s>`-bound pmodule is rejected at materialisation — the
     inner binder would shadow the outer one and silently decouple the
     invariant from per-handler state.
-/
import PLean

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 8

open PLean PartialCorrectness DemonicChoice

/-! ## Probe 1 — a false invariant correctly fails.

`always_x_is_42` is not preserved by a handler that increments `x`,
so SMT must report `1 failed`. -/

pmodule SoundnessR1
  system s

  event eGo
  machine Bad {
    var x : Nat
    start state Act {
      on eGo { x = x + 1 }
    }
  }

  Theorem broken {
    invariant always_x_is_42 :
      ∀ b : Bad, Bad_allocated b.ref s →
        (s.machines b.ref).fields.Bad_x = 42
  }

  Proof Safety {
    prove broken ;
  }
end SoundnessR1

#gen_module SoundnessR1

-- Pin the emitted shape: invariants are state-parameterised.
/-- info: SoundnessR1.always_x_is_42 : GlobalState SoundnessR1.Sig → Prop -/
#guard_msgs in
#check @SoundnessR1.always_x_is_42

-- Pin the failure count via the warning line. If the soundness fix
-- regresses (false invariant gets "verified"), the warning vanishes
-- and `#guard_msgs (severity := warning)` flags the mismatch. Both
-- the base-case VC and the inductive step disprove for `always_x_is_42`:
-- - base case: `InitConditions s → always_x_is_42 s` — at init,
--   nothing constrains `(s.machines b.ref).fields.Bad_x` to be `42`.
-- - inductive step: the handler increments `x`, breaking the invariant.
/--
warning: SoundnessR1: 1 proved by SMT, 0 user-proved, 2 disproved, 0 unknown, 0 tactic-error, 0 no-diagnostic, 0 missing-premise
2 obligation(s) need a manual proof; fill in the skeletons above.
---
warning: declaration uses 'sorry'
---
warning: declaration uses 'sorry'
-/
#guard_msgs (warning, drop info) in
set_option pverify.failOnIncomplete false in
#pverify SoundnessR1

/-! ## Probe 2 — `∀ s : GlobalState Sig, …` inside a `system`-bound invariant is rejected.

Under `system s`, the invariant materialises to `fun s => <body>`. An
inner `∀ s : GlobalState Sig, …` would shadow the outer `s` and
re-introduce the soundness hole — the materialiser rejects it. -/

pmodule SoundnessR2
  system s
  event eGo
  machine M {
    start state S { on eGo { pure () } }
  }

  Theorem broken_shape {
    invariant bad_shape : ∀ s : GlobalState Sig, True
  }
end SoundnessR2

-- The error fires at materialisation time (`#gen_module`), not at
-- registration: the `Theorem` block records a `defStx` that
-- `materialiseInvariant` rejects.
/--
error: invariant `bad_shape` body names the `GlobalState` type. A `∀`/`∃`/`let`/`have`/`fun` binder of type `GlobalState <Sig>` shadows the pmodule's `system` state binder and silently decouples the invariant from per-handler state (a soundness hole). Reference the pmodule's `system`-declared state binder directly in the body instead of introducing a new `GlobalState`-typed variable.
-/
#guard_msgs in
#gen_module SoundnessR2

/-! ## Probe 3 — non-leading `∀ s : GlobalState Sig, …` is also rejected.

The earlier rejector matched only the leading binder shape, so a body
like `True ∧ (∀ s : GlobalState Sig, P)` evaded detection and silently
re-introduced the soundness hole. The fix walks the body Syntax
recursively. -/

pmodule SoundnessR3
  system s
  event eGo
  machine M {
    start state S { on eGo { pure () } }
  }

  Theorem nested_shape {
    invariant bad_nested : True ∧ (∀ s : GlobalState Sig, True)
  }
end SoundnessR3

/--
error: invariant `bad_nested` body names the `GlobalState` type. A `∀`/`∃`/`let`/`have`/`fun` binder of type `GlobalState <Sig>` shadows the pmodule's `system` state binder and silently decouples the invariant from per-handler state (a soundness hole). Reference the pmodule's `system`-declared state binder directly in the body instead of introducing a new `GlobalState`-typed variable.
-/
#guard_msgs in
#gen_module SoundnessR3

/-! ## Probe 4 — `let`/`have` GlobalState shadow is rejected.

The ∀-only guard missed a `let s : GlobalState Sig := default; …`
binder (elaborates to `have`), which shadows the pmodule's `system`-
declared `s` and makes the invariant state-independent — a *false*
property would then verify as a clean pass (audit-confirmed
2026-06-19). The generalised guard rejects any `GlobalState` mention
in the body. -/

pmodule SoundnessR4
  system s
  event eGo
  machine Bad {
    var x : Nat
    start state Act { on eGo { x = x + 1 } }
  }

  Theorem broken_let {
    invariant let_shadow :
      let s : GlobalState Sig := default;
      (s.machines (default : MachineRef)).fields.Bad_x = 0
  }
end SoundnessR4

/--
error: invariant `let_shadow` body names the `GlobalState` type. A `∀`/`∃`/`let`/`have`/`fun` binder of type `GlobalState <Sig>` shadows the pmodule's `system` state binder and silently decouples the invariant from per-handler state (a soundness hole). Reference the pmodule's `system`-declared state binder directly in the body instead of introducing a new `GlobalState`-typed variable.
-/
#guard_msgs in
#gen_module SoundnessR4

/-! ## Probe 5 — `∃ s : GlobalState Sig, …` shadow is rejected.

The companion of probe 4: an existential binder of the state type also
slipped the ∀-only guard. The generalised guard catches it. -/

pmodule SoundnessR5
  system s
  event eGo
  machine M {
    start state S { on eGo { pure () } }
  }

  Theorem broken_exists {
    invariant exists_shadow : ∃ s : GlobalState Sig, s.actionCount = 0
  }
end SoundnessR5

/--
error: invariant `exists_shadow` body names the `GlobalState` type. A `∀`/`∃`/`let`/`have`/`fun` binder of type `GlobalState <Sig>` shadows the pmodule's `system` state binder and silently decouples the invariant from per-handler state (a soundness hole). Reference the pmodule's `system`-declared state binder directly in the body instead of introducing a new `GlobalState`-typed variable.
-/
#guard_msgs in
#gen_module SoundnessR5

/-! ## Probe 6 — a `sorry`-backed `@[pverifyProof]` is NOT a pass.

The obligation generator type-checks a manual proof by `exact @userThm`,
but `hasSorry` on that delegating `_check` value never sees through to
the user theorem's body. A `@[pverifyProof] … := by sorry` would then be
reported as `user-proved` (audit-confirmed 2026-06-19). The fix also
inspects the user theorem's own value; a sorried proof is reported as a
failure (`no-diagnostic`), so `#pverify` does not falsely pass. -/

pmodule SoundnessR6
  system s
  event eGo
  machine M {
    var x : Bool
    start state Act { on eGo { x = true } }
  }
  init-holds ∀ m : M, m.x = false

  Theorem broken_manual {
    invariant always_false : ∀ m : M, m.x = false
  }
  Proof { prove broken_manual ; }
end SoundnessR6

#gen_module SoundnessR6

namespace SoundnessR6
open PartialCorrectness DemonicChoice

-- A deliberately-sorried manual proof of the (false) obligation. Its
-- statement type-checks, but the proof is a hole.
@[pverifyProof]
theorem M.Act.eGo_correct_block0_broken_manual (this : M) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s => (broken_manual s) ∧ inflight lbl s ∧ lbl.target = this.ref ∧
        is_M this.ref s ∧ (s.machines this.ref).currentState = M.Act_st ∧
        lbl.action = .event E.eGo)
      (do PLean.markReceived (P := Sig) lbl; M.Act.eGo_handler this)
      (fun _ s => broken_manual s) := by
  sorry
end SoundnessR6

-- The sorried manual proof is counted as a failure (`no-diagnostic`),
-- NOT as `user-proved`. Key pin: `user-proved` stays 0. If the fix
-- regresses, the `eGo_correct_block0_broken_manual` obligation flips to
-- `user-proved` (1) and the summary line mismatches. (The base case and
-- auto-default also fail — `always_false` is genuinely non-inductive —
-- so all three are `no-diagnostic`.)
/--
warning: SoundnessR6: 0 proved by SMT, 0 user-proved, 0 disproved, 0 unknown, 0 tactic-error, 3 no-diagnostic, 0 missing-premise
3 obligation(s) need a manual proof; fill in the skeletons above.
-/
#guard_msgs (warning, drop info) in
set_option pverify.failOnIncomplete false in
#pverify SoundnessR6

-- Handler-coverage soundness regressions (goto-only handlers and entry
-- handlers) live in a separate file: probes 2–5 in this file
-- deliberately leave `#gen_module` failing midway, which leaves the
-- namespace stack non-empty for the rest of the file. The goto/entry
-- probes need a clean stack to resolve handler def names via the
-- standard `unfold M.S.ev_handler` chain, so they get their own
-- isolated test module — see `Tests/Syntax/HandlerCoverage.lean`.
