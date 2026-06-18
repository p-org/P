/-
Side-by-side regression for the two `#pverify` paths:

- Without `@[pverifyProof]`: auto path emits `theorem ... := by pverify`.
- With `@[pverifyProof]`: registry lookup short-circuits emission and
  the user's proof stands.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

/-! ## Case A — without `@[pverifyProof]`, the auto path runs. -/

pmodule PVerifyAutoOnly

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

end PVerifyAutoOnly

#gen_module PVerifyAutoOnly
-- Expected: 2 obligations (`prove trivial` + auto `prove default`),
-- both close via the auto path.
#pverify PVerifyAutoOnly

/-! ## Case B — with `@[pverifyProof]`, the manual path runs. -/

pmodule PVerifyWithManual

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

end PVerifyWithManual

#gen_module PVerifyWithManual

namespace PVerifyWithManual

/-- Manual proof for the `prove trivial` obligation. -/
@[pverifyProof]
theorem M.S.eHello_correct_Safety_trivial (this : M) (lbl : Sig.Label) :
    triple (l := PProp Sig)
      (fun s =>
        (trivial s ∧ True) ∧
        PLean.inflight lbl s ∧
        lbl.target = this.ref ∧
        is_M this.ref s ∧
        (s.machines this.ref).currentState = M.S_st ∧
        lbl.action = .event E.eHello)
      (do PLean.markReceived (P := Sig) lbl; M.S.eHello_handler this)
      (fun _ s =>
        trivial s ∧ True) := by
  unfold M.S.eHello_handler trivial always_true
  pverify_step_wp

end PVerifyWithManual

-- Expected: `prove trivial` user-proved, auto `prove default` SMT-proved.
#pverify PVerifyWithManual
