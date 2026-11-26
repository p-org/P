/-
End-to-end: a `foreach` broadcast whose sends are described by the loop
invariant, and a `while` loop over a local counter.
-/
import PLean

open PLean

pmodule LoopT

  system s

  type tReq = (origin : PLean.MachineRef)

  event eReq : tReq
  event eAck : tReq

  function peers : seq[PLean.MachineRef]
  paxiom peers_machines : ∀ m : PLean.MachineRef, m ∈ peers → m ≠ PLean.MachineRef.null

  machine Node {
    var acks : Int

    start state Init {
      entry {
        foreach (m in peers)
          invariant only_req :
            new_sends e => e is eReq ∧ (eReq_payload_of e).origin = e.src ∧ e.src = hs.ref ;
        {
          send m, eReq, (origin = this.ref)
        }
        var i : Int
        i = 0
        while (i < 3)
          invariant bounded : i ≤ 3 ;
          decreasing (3 - i).toNat ;
        {
          i = i + 1
        }
      }

      on eReq (payload : tReq) {
        send payload.origin, eAck, (origin = this.ref)
      }

      on eAck (payload : tReq) {
        acks = acks + 1
      }
    }
  }

  init-holds ∀ n : Node, n.acks = 0

  Theorem safety {
    invariant req_origin : ∀ e : eReq, sent e s → e.origin = e.src
    invariant req_from_node : ∀ e : eReq, sent e s → is_Node e.src s
    invariant ack_origin : ∀ e : eAck, sent e s → e.origin = e.src
    invariant acks_nonneg : ∀ n : Node, n.acks ≥ 0
  }
  Proof {
    prove safety ;
  }

end LoopT

#gen_module LoopT

#pverify LoopT

/-! Iterating a set: the loop variable is a member of the set. -/

pmodule LoopSetT
  system s
  event eReq
  function peers : set[PLean.MachineRef]
  paxiom peers_machines : ∀ m : PLean.MachineRef, m ∈ peers → m ≠ PLean.MachineRef.null
  machine Node {
    start state Init {
      entry {
        foreach (m in peers)
          invariant to_peers : new_sends e => e.target ∈ peers ;
        {
          send m, eReq
        }
      }
      ignore eReq
    }
  }
  Theorem safety {
    invariant req_to_peers : ∀ e : eReq, sent e s → e.target ∈ peers
  }
  Proof { prove safety ; }
end LoopSetT

#gen_module LoopSetT

#pverify LoopSetT
