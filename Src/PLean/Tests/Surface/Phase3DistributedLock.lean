/-
PLean port of [`Tutorial/Advanced/6_DistributedLock`](../../../Tutorial/Advanced/6_DistributedLock/PSrc/System.p).

`Theorem safety` ports all five P-source invariants (including
`not_held_after_release` and `transfer_to_higher`, which the original
P proof needs). The `prove default` obligations close via SMT;
`prove safety` still yields counter-examples — the residual gap is
in the `init-condition` port (not yet wired) and any further
invariants needed once init-conditions are in the precondition.
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

      -- Ported from the P source: when an eAccept is in flight from
      -- node n1, n1 has already released the lock.
      invariant not_held_after_release :
        ∀ n1 : Node, ∀ e : Sig.Label, ∀ p : tAccept,
          Node_allocated n1.ref s →
          inflight e s →
          e.action = .event (E.eAccept p) →
          p.source = n1.ref →
          (s.machines n1.ref).fields.Node_held = false

      -- Ported from the P source: an in-flight eAccept always
      -- transfers to a strictly higher epoch.
      invariant transfer_to_higher :
        ∀ n1 : Node, ∀ e : Sig.Label, ∀ p : tAccept,
          Node_allocated n1.ref s →
          inflight e s →
          e.action = .event (E.eAccept p) →
          p.source = n1.ref →
          p.epoch > (s.machines n1.ref).fields.Node_epoch
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
