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

-- Pin the `1 failed` count. If it flips to `0 failed`, the soundness
-- fix has regressed (the false invariant got "verified").
/--
info: SoundnessR1: well-formed (0 types, 1 events, 1 machines, 1 invariants, 0 axioms, 0 instances)
---
warning: obligation incomplete for Bad.Act.eGo (lemma broken); SMT could not close. Write a `@[pverifyProof] theorem Bad.Act.eGo_correct_Safety_broken : ... := by ...` to supply a manual proof.
---
warning: SoundnessR1: 2 obligations from 1 prove-directives (1 proved by SMT, 0 user-proved, 1 failed)
Failed: Bad.Act.eGo (lemma broken)
Write a manual proof for each via `@[pverifyProof]` (paste these names verbatim):
  @[pverifyProof] theorem Bad.Act.eGo_correct_Safety_broken := by sorry  -- supply manual proof
---
warning: declaration uses 'sorry'
---
info: Goal proven by cvc5. Trusting SMT solver result.
-/
#guard_msgs in
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
error: invariant `bad_shape` is inside a `system` block but its body begins with `∀ <ident> : GlobalState <Sig>, …`. That inner ∀-binder shadows the outer `system` state binder and silently decouples the invariant from per-handler state (the soundness hole fixed 2026-06-10). Drop the leading `∀ … : GlobalState Sig,` and reference the `system`-block's state binder directly in the body.
-/
#guard_msgs in
#gen_module SoundnessR2
