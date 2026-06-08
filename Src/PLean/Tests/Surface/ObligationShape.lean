/-
PLean Phase-3 — `#guard_msgs`-pinned obligation-shape regression
(REVIEW_P3 §6.7). Records the exact `theorem` types `#pverify` emits
so a future refactor of `Verify/Obligation.lean::emitOneObligation`
can't silently change the obligation shape (precondition,
postcondition, dispatcher contract, theorem-name suffix scheme).

We use a tiny pmodule with two `Proof` blocks targeting the same
lemma — exercising the §A.1 theorem-name disambiguation (proof tag
embedded in the name) — and check the resulting theorem signatures
via `#check`. If the generator changes pre/post/lemma-bundle plumbing,
the `#check` output drifts and `#guard_msgs` fails.

We deliberately use a `pure ()` handler so the obligations close
trivially under `pverify`'s present automation (the shape regression
is about the *theorem statement*, not whether it discharges).
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule ObligationShapeMod

  event eHello

  machine M {
    start state S {
      on eHello { pure () }
    }
  }

  Theorem safety {
    invariant always_true : ∀ s : GlobalState Sig, True
  }

  Proof Block1 {
    prove safety ;
  }

  Proof Block2 {
    prove default ;
  }

end ObligationShapeMod

#gen_module ObligationShapeMod
#pverify    ObligationShapeMod

namespace ObligationShapeMod

/-! ## Pinned obligation-shape signatures

If any of these `#check` outputs change, the obligation generator
has shifted shape — see [`Verify/Obligation.lean`](../../PLean/Verify/Obligation.lean).
The expected names embed the Proof-block tag (`Block1` / `Block2`)
per the §A.1 fix so two `Proof` blocks targeting the same handler
no longer collide. -/

/--
info: M.S.eHello_correct_Block1_safety : ∀ (this : M),
  triple
    (fun s ↦
      (safety s ∧ DefaultInvariants s ∧ InitConditions s ∧ True) ∧
        ∃ lbl,
          inflight lbl s ∧
            lbl.target = this.ref ∧
              (s.machines this.ref).currentState = M.S_st ∧ lbl.action = EventOrGoto.event E.eHello)
    (M.S.eHello_handler this) fun x s ↦ safety s ∧ DefaultInvariants s ∧ InitConditions s ∧ True
-/
#guard_msgs in
#check @M.S.eHello_correct_Block1_safety

/--
info: M.S.eHello_correct_Block2_default : ∀ (this : M),
  triple
    (fun s ↦
      (DefaultInvariants s ∧ DefaultInvariants s ∧ InitConditions s ∧ True) ∧
        ∃ lbl,
          inflight lbl s ∧
            lbl.target = this.ref ∧
              (s.machines this.ref).currentState = M.S_st ∧ lbl.action = EventOrGoto.event E.eHello)
    (M.S.eHello_handler this) fun x s ↦ DefaultInvariants s ∧ DefaultInvariants s ∧ InitConditions s ∧ True
-/
#guard_msgs in
#check @M.S.eHello_correct_Block2_default

end ObligationShapeMod
