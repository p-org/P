/-
PLean port of [`Tutorial/Advanced/6_DistributedLock`](../../../Tutorial/Advanced/6_DistributedLock/PSrc/System.p).

`Theorem safety` ports all five P-source invariants (including
`not_held_after_release` and `transfer_to_higher`); the P source's
`init-condition` ("exactly one Node holds the lock at startup") is
ported as the `init-holds` clause below. All ten base-case obligations
and both `prove default` inductive obligations discharge via SMT; the
two `prove safety` inductive-step obligations remain disproved — a
genuine inductiveness gap across the `eGrant`/`eAccept` handlers.

Quantifiers over a machine kind (`∀ n : Node, …`) auto-inject
runtime kind guards (`is_Node n.ref s →`) at materialisation, so the
user doesn't have to spell out `Node_allocated n.ref s →` manually
after every `Node`-quantified binder.
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

  -- Ported from the P source's `init-condition`: at startup exactly
  -- one Node holds the lock (with a positive epoch); every other Node
  -- has `held = false` and `epoch = 0`. The `∀ n : Node` / `∃ n : Node`
  -- quantifiers auto-inject `is_Node n.ref s` guards at materialisation
  -- — bare `n : Node` value would otherwise admit refs whose state
  -- slot is unallocated or has a different kind.
  init-holds (
    ∃ n : Node,
      (s.machines n.ref).fields.Node_held = true ∧
      (s.machines n.ref).fields.Node_epoch > 0 ∧
      ∀ n1 : Node,
        n1 ≠ n →
        (s.machines n1.ref).fields.Node_held = false ∧
        (s.machines n1.ref).fields.Node_epoch = 0)

  Theorem safety {
    system s {
      invariant unique_holder :
        ∀ n1 n2 : Node,
          (s.machines n1.ref).fields.Node_held = true →
          (s.machines n2.ref).fields.Node_held = true →
          n1 = n2

      invariant no_lock_while_transfer :
        ∀ n : Node, ∀ e : eAccept,
          inflight e s →
          (s.machines n.ref).fields.Node_held = false

      invariant unique_accept :
        ∀ e1 e2 : eAccept,
          inflight e1 s → inflight e2 s → e1 = e2

      -- Ported from the P source: when an eAccept is in flight from
      -- node n1, n1 has already released the lock.
      invariant not_held_after_release :
        ∀ n1 : Node, ∀ e : Sig.Label, ∀ p : tAccept,
          inflight e s →
          e.action = .event (E.eAccept p) →
          p.source = n1.ref →
          (s.machines n1.ref).fields.Node_held = false

      -- Ported from the P source: an in-flight eAccept always
      -- transfers to a strictly higher epoch.
      invariant transfer_to_higher :
        ∀ (n1 : Node) (e : Sig.Label) (p : tAccept),
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
