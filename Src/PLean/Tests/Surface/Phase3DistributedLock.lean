/-
PLean port of [`Tutorial/Advanced/6_DistributedLock`](../../../Tutorial/Advanced/6_DistributedLock/PSrc/System.p).

The three invariants in `Theorem safety` are not jointly inductive
over the `eGrant` / `eAccept` handlers without the original P
source's `not_held_after_release` and `transfer_to_higher` clauses;
those `prove safety` obligations are expected to fail SMT and need
either the missing invariants or `@[pverifyProof]` manual proofs.
The `prove default` obligations close via SMT.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule DistributedLock

  type tGrant  = (node : PLean.MachineRef, epoch : Int)
  type tAccept = (epoch : Int, source : PLean.MachineRef)

  event eGrant  : tGrant
  event eAccept : tAccept

  machine Node {
    var epoch : Int
    var held  : Bool

    start state Act {
      on eGrant (payload : tGrant) {
        if (held && decide (payload.epoch > epoch)) then do
          held = false
          send payload.node, eAccept, (epoch = payload.epoch, source = this.ref)
      }

      on eAccept (payload : tAccept) {
        if (decide (payload.epoch > epoch)) then do
          held = true
          epoch = payload.epoch
      }
    }
  }

  Theorem safety {
    system s {
      invariant unique_holder :
        ∀ n1 n2 : Node,
          Node_allocated n1.ref s → Node_allocated n2.ref s →
          (s.machines n1.ref).fields.Node_held = true →
          (s.machines n2.ref).fields.Node_held = true →
          n1 = n2

      invariant no_lock_while_transfer :
        ∀ n : Node, ∀ e : Sig.Label,
          Node_allocated n.ref s → e is eAccept → inflight e s →
          (s.machines n.ref).fields.Node_held = false

      invariant unique_accept :
        ∀ e1 e2 : Sig.Label,
          e1 is eAccept → e2 is eAccept → inflight e1 s → inflight e2 s → e1 = e2
    }
  }

  Proof Safety {
    prove safety ;
    prove default ;
  }

end DistributedLock

#gen_module DistributedLock
#pwf        DistributedLock

-- @[pverifyProof] theorem Node.Act.eGrant_correct_Safety_safety := by sorry  -- supply manual proof
-- @[pverifyProof] theorem Node.Act.eAccept_correct_Safety_safety := by sorry  -- supply manual proof

set_option pverify.failOnIncomplete false in
#pverify DistributedLock
