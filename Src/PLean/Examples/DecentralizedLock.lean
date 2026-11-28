/-
DecentralizedLock: DuoAI's Ivy model `decentralized_lock.ivy` (`simple-de-lock`). A lock
token passes between nodes by messages; unlike `DistributedLock`, there are no epochs and a
node accepts every transfer.

`nodes` is the set of Nodes, and initially exactly one of them holds the lock (Ivy's
`start_node`). The holder gives the lock up by sending an `eTransfer` to any node, itself
included; the receiver then holds it.

Ivy's spontaneous `send(src, dst)` is driven by requests: a Node that comes to hold the lock
(its start entry, or the entry it re-enters after a transfer) sends itself one `eRelease`
per node. A request delivered to the holder hands the lock to its `dst`; one delivered to a
non-holder does nothing. The delivery order picks the destination, so every `dst` stays
available, and holding forever is the run that delivers none. (`choose` from `nodes` would
need `sizeof(nodes) > 0`, which the solvers cannot derive from a member.)

Deviations from the Ivy model:
- The requests and the entry that sends them are not in Ivy. They change no state Ivy has,
  and a stale request delivered to a later holder is a valid Ivy `send`.
- Ivy's `message` is a set of pairs, PLean's network a bag. They differ only when a transfer
  is sent while another is in flight, which never happens (`unique_transfer`).
- Ivy's sort `node` may be infinite, machines are finitely many. A run that violates safety
  involves finitely many nodes, so finite instances lose no counterexample.

Safety is Ivy's `X ~= Y -> ~(has_lock(X) & has_lock(Y))` (`mutex`). `DecentralizedLock.safe`
proves it in every world reachable under bag delivery; `safe_fifo` and `safety_holds` cover
FIFO delivery only, a subset of Ivy's runs.

Proof: `mutex` is inductive together with `Lemma token` (while a transfer is in flight,
nobody holds the lock). Every obligation is proved automatically.
-/
import PLean

open PLean

pmodule DecentralizedLock

  system s

  type tRelease = (dst : PLean.MachineRef)

  event eRelease  : tRelease
  event eTransfer

  function nodes : set[PLean.MachineRef]

  machine Node {
    var has_lock : Bool

    start state Run {
      entry {
        if has_lock then do
          foreach (d in nodes)
            invariant offers :
              new_sends e => e is eRelease ∧ e.target = this.ref ∧
                (eRelease_payload_of e).dst ∈ nodes ;
          {
            send this.ref, eRelease, (dst = d)
          }
      }

      on eRelease (r : tRelease) {
        if has_lock then do
          has_lock = false
          send r.dst, eTransfer
      }

      on eTransfer {
        has_lock = true
        goto Run
      }
    }
  }

  init-holds ∀ r : PLean.MachineRef, r ∈ nodes ↔ is_Node r s
  init-holds ∃ h : Node, ∀ n : Node, n.has_lock = true ↔ n.ref = h.ref

  -- Requests name Nodes, so `eRelease` never sends to `null`.
  Lemma config {
    invariant nodes_set : ∀ r : PLean.MachineRef, r ∈ nodes ↔ is_Node r s
    invariant release_to_node : ∀ e : eRelease, inflight e s → e.dst ∈ nodes
  }

  Lemma token {
    invariant transfer_no_holder :
      ∀ (n : Node) (e : eTransfer), inflight e s → n.has_lock = false
    invariant unique_transfer :
      ∀ e1 e2 : eTransfer, inflight e1 s → inflight e2 s → e1 = e2
  }

  Theorem safety {
    invariant mutex :
      ∀ n1 n2 : Node, n1 ≠ n2 → ¬ (n1.has_lock = true ∧ n2.has_lock = true)
  }

  Proof Safety {
    prove config ;
    prove token using safety ;
    prove safety using token ;
  }

end DecentralizedLock

#gen_module DecentralizedLock
#pwf        DecentralizedLock

#pverify DecentralizedLock
