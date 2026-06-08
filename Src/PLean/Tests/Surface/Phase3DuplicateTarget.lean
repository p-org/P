/-
PLean Phase-3 — explicit positive regression for the §A.1 theorem-name
disambiguation (REVIEW_P3 second-pass §2.3).

Two `Proof` blocks both target lemma `safety` with no `using`-clause.
Before the §A.1 fix, both directives produced
`<M>.<S>.<ev>_correct_safety` and the second emit collided in the
environment, surfacing as a "failed obligation" rather than as the
two-block resolution the user wrote. After the fix, the Proof-block
tag (`Block1` / `Block2`) is embedded in the theorem name and both
directives produce distinct, well-formed theorems.

This file proves the disambiguation works *positively* (both theorem
names exist after `#pverify`); `ObligationShape.lean` covers it
implicitly via `#guard_msgs` on the emitted signatures.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule Phase3DuplicateTargetMod

  event eHello

  machine M {
    start state S {
      on eHello { pure () }
    }
  }

  Theorem safety {
    invariant always_true : ∀ s : GlobalState Sig, True
  }

  -- Two `Proof` blocks targeting the same lemma. Tags are required
  -- (anonymous blocks fall back to `block<idx>`, which would also
  -- disambiguate, but the explicit tags exercise the more interesting
  -- code path).
  Proof Block1 {
    prove safety ;
  }

  Proof Block2 {
    prove safety ;
  }

end Phase3DuplicateTargetMod

#gen_module Phase3DuplicateTargetMod
#pverify    Phase3DuplicateTargetMod

namespace Phase3DuplicateTargetMod

-- Both theorems must exist with the Proof-block-tag-disambiguated names.
-- If the §A.1 disambiguation regresses, one of these `#check`s will
-- fail with `unknown identifier`.
#check @M.S.eHello_correct_Block1_safety
#check @M.S.eHello_correct_Block2_safety

end Phase3DuplicateTargetMod
