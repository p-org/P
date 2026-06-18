/-
Soundness regressions for state-implicit invariants:

  1. A genuinely-false invariant must FAIL to verify. (Pins that the
     bundle predicate `fun s => name s ∧ …` actually applies `s`,
     rather than ignoring it.)

  2. Invariants are emitted as `GS → Prop` (not closed `Prop`), so
     bare state references inside the body resolve to the bundle's
     state argument.

  3. An inner `∀ s : GlobalState Sig, …` inside a `system <s> { … }`
     block is rejected at materialisation — the inner binder would
     shadow the outer one and silently decouple the invariant from
     per-handler state.
-/
import PLean

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 8

open PLean PartialCorrectness DemonicChoice

/-! ## Probe 1 — a false invariant correctly fails.

`always_x_is_42` is not preserved by a handler that increments `x`,
so SMT must report `1 failed`. -/

pmodule SoundnessR1
  event eGo
  machine Bad {
    var x : Nat
    start state Act {
      on eGo { x = x + 1 }
    }
  }

  Theorem broken {
    system s {
      invariant always_x_is_42 :
        ∀ b : Bad, Bad_allocated b.ref s →
          (s.machines b.ref).fields.Bad_x = 42
    }
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
warning: SoundnessR1: 1 proved by SMT, 0 user-proved, 2 disproved, 0 unknown, 0 tactic-error, 0 no-diagnostic
2 obligation(s) need a manual proof; fill in the skeletons above.
---
warning: declaration uses 'sorry'
---
warning: declaration uses 'sorry'
-/
#guard_msgs (warning, drop info) in
set_option pverify.failOnIncomplete false in
#pverify SoundnessR1

/-! ## Probe 2 — `∀ s : GlobalState Sig, …` inside `system s { … }` is rejected.

Outside a `system` block, the binder is the wildcard `_`, so an
explicit `∀ s : GlobalState Sig, …` is harmless (defines a
state-independent meta-property). Inside `system s { … }`, the inner
∀ would shadow the outer binder and reintroduce the soundness hole —
the materialiser rejects it. -/

pmodule SoundnessR2
  event eGo
  machine M {
    start state S { on eGo { pure () } }
  }

  Theorem broken_shape {
    system s {
      invariant bad_shape : ∀ s : GlobalState Sig, True
    }
  }
end SoundnessR2

-- The error fires at materialisation time (`#gen_module`), not at
-- registration: the `Theorem` block records a `defStx` that
-- `materialiseInvariant` rejects.
/--
error: invariant `bad_shape` lives inside a `system` block but its body contains `∀ <ident> : GlobalState <Sig>, …`. That inner ∀-binder shadows the outer `system` state binder and silently decouples the invariant from per-handler state (the soundness hole fixed 2026-06-10). Drop the inner `∀ … : GlobalState Sig,` and reference the `system`-block's state binder directly in the body.
-/
#guard_msgs in
#gen_module SoundnessR2

/-! ## Probe 3 — non-leading `∀ s : GlobalState Sig, …` is also rejected.

The earlier rejector matched only the leading binder shape, so a body
like `True ∧ (∀ s : GlobalState Sig, P)` evaded detection and silently
re-introduced the soundness hole. The fix walks the body Syntax
recursively. -/

pmodule SoundnessR3
  event eGo
  machine M {
    start state S { on eGo { pure () } }
  }

  Theorem nested_shape {
    system s {
      invariant bad_nested : True ∧ (∀ s : GlobalState Sig, True)
    }
  }
end SoundnessR3

/--
error: invariant `bad_nested` lives inside a `system` block but its body contains `∀ <ident> : GlobalState <Sig>, …`. That inner ∀-binder shadows the outer `system` state binder and silently decouples the invariant from per-handler state (the soundness hole fixed 2026-06-10). Drop the inner `∀ … : GlobalState Sig,` and reference the `system`-block's state binder directly in the body.
-/
#guard_msgs in
#gen_module SoundnessR3
