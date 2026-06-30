/-
Loop invariants that name user-declared bundle names reach SMT in
unfolded form. The obligation generator unfolds bundle names + their
individual invariants in the iteration VC's invariant list.

Pin two shapes:

(A) `MachineRef`-quantified bundle. User-invariant closes via SMT;
    auto-default disproves with a counter-example (loop body's `send`
    doesn't preserve `DefaultInvariants` without a stronger user
    invariant — strengthen by adding `invariant inv_default :
    DefaultInvariants s ;` to the loop).

(B) Machine-wrapper-quantified bundle. Bundle body keeps wrapper
    type at the surface; under a loop invariant body, lean-auto can
    fabricate two `Worker` values with equal `ref` but distinct
    identity, producing an SMT counter-example on the user-invariant
    obligation. To close, use a `MachineRef`-quantified invariant
    (`∀ m : MachineRef, is_Worker m s → …`) — `Tests/Verify/LoopInvStrong`
    pins the working shape.
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

  -- Surface bundle body quantifies over the machine WRAPPER type.
  -- Surface-invariant materialisation keeps the wrapper type (the
  -- rewrite is opt-in for loop invariants only).
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
