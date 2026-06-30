/-
Multi-invariant loops: when the user supplies invariants strong enough
to discharge the post, SMT closes the iteration VC automatically.

The wrapper→MachineRef rewrite fires inside loop-invariant bodies (via
`pLoopInvWrap%`) so `∀ p : <wrapper>, …` quantifiers in inline loop
invariants don't trip lean-auto's monomorphizer under the iteration VC.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 5

/-! ## `DefaultInvariants` as a loop invariant closes the `prove default`
    obligation. -/

pmodule StrongLoopInv
  system s
  event eTick : PLean.MachineRef
  function preference : PLean.MachineRef → Bool

  machine Worker {
    var peers : seq[PLean.MachineRef]
    start state Idle {
      on eTick (p : PLean.MachineRef) {
        foreach (q in peers)
          invariant inv_default : DefaultInvariants s ;
        {
          send q, eTick, p
        }
      }
    }
  }

  Proof of_default { prove default ; }
end StrongLoopInv

#gen_module StrongLoopInv
#pverify    StrongLoopInv

/-! ## Multiple loop invariants. The body of one quantifies over a
    machine-wrapper type — the wrapper→MachineRef rewrite fires inside
    the loop-invariant elaboration so the iteration VC translates. -/

pmodule WrapperLoopInv
  system s
  event eTick : PLean.MachineRef
  function preference : PLean.MachineRef → Bool

  machine Worker {
    var peers : seq[PLean.MachineRef]
    start state Idle {
      on eTick (p : PLean.MachineRef) {
        foreach (q in peers)
          invariant inv_default : DefaultInvariants s ;
          invariant inv_wf : ∀ w : Worker,
            preference w.ref = true ∨ preference w.ref = false ;
        {
          send q, eTick, p
        }
      }
    }
  }

  Proof of_default { prove default ; }
end WrapperLoopInv

#gen_module WrapperLoopInv
#pverify    WrapperLoopInv
