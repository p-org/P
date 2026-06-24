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
theorem M.S.eHello_correct_Safety_trivial (this : M) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s =>
        (trivial s) ∧
        PLean.inflight lbl s ∧
        lbl.target = this.ref ∧
        is_M this.ref s ∧
        (s.machines this.ref).currentState = M.S_st ∧
        lbl.action = .event E.eHello)
      (do PLean.markReceived (P := Sig) lbl; M.S.eHello_handler this)
      (fun _ s =>
        trivial s) := by
  -- The dispatched label `lbl` is now a binder; the program marks it
  -- received then runs the handler. Handler is `pure ()` and the invariant
  -- is `always_true := True`, so once the bundle is unfolded the post
  -- `True` closes during `pverify_step_wp`'s own simp.
  unfold M.S.eHello_handler trivial always_true
  pverify_step_wp

end PVerifyManualProofDemo

#pverify PVerifyManualProofDemo
