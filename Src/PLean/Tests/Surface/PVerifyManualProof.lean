/-
End-to-end demo of the `@[pverifyProof]` manual-proof workflow.

The user writes a `theorem` whose name matches what the obligation
generator would emit, tags it `@[pverifyProof]`, and `#pverify` picks
it up on its registry walk and skips the auto-discharge for that
obligation.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule PVerifyManualProofDemo

  event eHello

  machine M {
    start state S {
      on eHello { pure () }
    }
  }

  Theorem trivial {
    invariant always_true : True
  }

  Proof Safety {
    prove trivial ;
  }

end PVerifyManualProofDemo

#gen_module PVerifyManualProofDemo

namespace PVerifyManualProofDemo
open PartialCorrectness DemonicChoice

/-- User-supplied manual proof. The theorem name MUST match the shape
`<M>.<S>.<ev>_correct_<proofTag>_<target>` the obligation generator
emits. -/
@[pverifyProof]
theorem M.S.eHello_correct_Safety_trivial (this : M) :
    triple (l := PProp Sig)
      (fun s =>
        (trivial s ∧ DefaultInvariants s ∧ True) ∧
        ∃ lbl : Sig.Label,
          PLean.inflight lbl s ∧
          lbl.target = this.ref ∧
          (s.machines this.ref).currentState = M.S_st ∧
          lbl.action = .event E.eHello)
      (M.S.eHello_handler this)
      (fun _ s =>
        trivial s ∧ DefaultInvariants s ∧ True) := by
  -- Step through the WP plumbing and unfold the handler / framework
  -- predicates. The atomic tactics in `Verify/Tactic.lean` are
  -- composable: each is a single-purpose step the user can chain.
  unfold M.S.eHello_handler
  pverify_step_wp
  intros
  -- Handler is `pure ()`; post conjunction matches pre-state hypotheses.
  refine ⟨?_, ?_⟩ <;> assumption

end PVerifyManualProofDemo

#pverify PVerifyManualProofDemo
