/-
PLean Phase-3 — port of Tutorial/Advanced/8_LockServer.

Status: surface parses, registry populates, `#gen_module` synthesises
types/machines (including `MKind` and `is_<M>` predicates per D20).
`#pverify` is held until R15 lands (see Phase3DistributedLock.lean).

The benchmark exercises:
- multiple machine kinds (`Server` and `Node`) with `m is Server` /
  `m is Node` membership predicates (D20),
- multiple `Lemma`/`Theorem` blocks chained via `prove safety using
  system_config` (D25),
- a `pure lock_server() : machine` foreign symbol — modelled here as
  an `opaque` pmodule member.

Reference: `Tutorial/Advanced/8_LockServer/PSrc/System.p`.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule LockServer

  type tLockSender   = (sender : PLean.MachineRef)
  type tUnlockSender = (sender : PLean.MachineRef)

  event eLock    : tLockSender
  event eUnlock  : tUnlockSender
  event eGrant
  event eAquire
  event eRelease

  -- The reference P source declares `pure lock_server(): machine` —
  -- modelled as an opaque pure constant.
  function lock_server : PLean.MachineRef

  machine Server {
    var has_lock : Bool

    start state Serving {
      on eLock (p : tLockSender) {
        if (has_lock) then do
          has_lock = false
          send p.sender, eGrant
      }

      on eUnlock (_p : tUnlockSender) {
        has_lock = true
      }
    }
  }

  machine Node {
    var has_lock : Bool
    var server   : PLean.MachineRef

    start state Working {
      on eAquire {
        send server, eLock, (sender = this.ref)
      }

      on eRelease {
        if (has_lock) then do
          has_lock = false
          send server, eUnlock, (sender = this.ref)
      }

      on eGrant {
        has_lock = true
      }
    }
  }

  Lemma system_config {
    invariant aquire_to_node :
      ∀ (s : GlobalState Sig) (e : Sig.Label) (mref : MachineRef),
        e is eAquire → (e targets mref) → (is_Server mref s) → ¬ inflight e s

    invariant release_to_node :
      ∀ (s : GlobalState Sig) (e : Sig.Label) (mref : MachineRef),
        e is eRelease → (e targets mref) → (is_Server mref s) → ¬ inflight e s

    invariant grant_to_node :
      ∀ (s : GlobalState Sig) (e : Sig.Label) (mref : MachineRef),
        e is eGrant → (e targets mref) → (is_Server mref s) → ¬ inflight e s
  }
  Proof {
    prove system_config ;
  }

  Theorem safety {
    invariant unique_lock_holder :
      ∀ s : GlobalState Sig, ∀ n1 n2 : Node,
        Node_allocated n1.ref s → Node_allocated n2.ref s →
        (s.machines n1.ref).fields.Node_has_lock = true →
        (s.machines n2.ref).fields.Node_has_lock = true →
        n1 = n2
  }
  Proof {
    prove safety using system_config ;
    prove default using system_config ;
  }

end LockServer

#gen_module LockServer
#pwf        LockServer

-- Phase-3 status: see file-level comment / R15.
-- #pverify LockServer
