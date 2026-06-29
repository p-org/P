/-
Probe: loop invariants that name user-declared bundle names should
reach SMT in a closed form. After the Obligation-gen fix (always
unfold lemmaBundleNames), the bundle reference unfolds before SMT.
But: if the bundle body quantifies over a **machine wrapper type**
(`∀ p : Participant, …`), lean-auto still rejects with
"Higher order input?" — wrapper-type quantifiers under a `pforeach`
iteration VC don't translate cleanly.

This file probes two cases to isolate the gap:

(A) `MachineRef`-quantified bundle: closes via SMT. The bundle
    unfolds, the iteration VC contains `∀ m : MachineRef, …`
    which is first-order.

(B) `MachineWrapper`-quantified bundle: fails with "Higher order
    input?". The wrapper-typed quantifier under the iteration VC
    is what triggers lean-auto's rejection.

The fix is principled and sound: VC gen should automatically
**reduce wrapper-quantified loop invariants to MachineRef-quantified
ones** at materialisation time, mirroring the way event quantifiers
get retyped to `Sig.Label` by `injectKindGuards`. This file pins
the current behaviour so the future fix is observable.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 5

/-! ## Probe A: MachineRef-quantified bundle (works today). -/

pmodule LoopInvBundleA

  system s

  event eTick : PLean.MachineRef

  function preference : PLean.MachineRef → Bool

  machine Broadcaster {
    var peers : seq[PLean.MachineRef]

    start state Idle {
      on eTick (p : PLean.MachineRef) {
        foreach (q in peers)
          invariant inv_bundle : flag_holds s ;
        {
          send q, eTick, p
        }
      }
    }
  }

  Lemma flag_holds {
    invariant fh_main :
      ∀ m : PLean.MachineRef, preference m = true ∨ preference m = false
  }

  Proof of_bundle {
    prove flag_holds ;
  }

end LoopInvBundleA

#gen_module LoopInvBundleA
#pwf        LoopInvBundleA

set_option pverify.failOnIncomplete false in
#pverify    LoopInvBundleA

/-! ## Probe B: machine-wrapper-quantified bundle (FAILS today). -/

pmodule LoopInvBundleB

  system s

  event eTick : PLean.MachineRef

  function preference : PLean.MachineRef → Bool

  machine Worker {
    var peers : seq[PLean.MachineRef]

    start state Idle {
      on eTick (p : PLean.MachineRef) {
        foreach (q in peers)
          invariant inv_bundle : wrapper_flag s ;
        {
          send q, eTick, p
        }
      }
    }
  }

  -- Bundle body quantifies over the machine WRAPPER type (`Worker`,
  -- not `MachineRef`). The kind-guard injection adds
  -- `is_Worker w.ref s →` but keeps `w : Worker`. This wrapper-
  -- type quantifier under the iteration VC trips lean-auto.
  Lemma wrapper_flag {
    invariant wf_main :
      ∀ w : Worker, preference w.ref = true ∨ preference w.ref = false
  }

  Proof of_bundle {
    prove wrapper_flag ;
  }

end LoopInvBundleB

#gen_module LoopInvBundleB
#pwf        LoopInvBundleB

set_option pverify.failOnIncomplete false in
#pverify    LoopInvBundleB
