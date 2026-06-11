/-
PLean port of [`Tutorial/Advanced/8_LockServer`](../../../Tutorial/Advanced/8_LockServer/PSrc/System.p).

Exercises:
- two machine kinds (`Server`, `Node`) with `m is Server` / `m is Node`,
- multi-lemma `using` chain (`prove safety using system_config`),
- a `pure lock_server() : machine` modelled as an opaque constant.

The lemma chain is not jointly inductive on its own; only the
`prove default` / trivial-handler obligations close via SMT. The
others need additional invariants or `@[pverifyProof]` manual proofs.
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

  -- P source has `pure lock_server() : machine`; modelled as an
  -- opaque constant (no body).
  function lock_server : PLean.MachineRef

  machine Server {
    var has_lock : Bool

    start state Serving {
      on eLock (p : tLockSender) {
        if (has_lock) then do
          has_lock = false;
          send p.sender, eGrant;
      }

      on eUnlock (_p : tUnlockSender) {
        has_lock = true;
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
    system s {
      invariant aquire_to_node :
        ∀ (e : Sig.Label) (mref : MachineRef),
          e is eAquire → (e targets mref) → (is_Server mref s) → ¬ inflight e s

      invariant release_to_node :
        ∀ (e : Sig.Label) (mref : MachineRef),
          e is eRelease → (e targets mref) → (is_Server mref s) → ¬ inflight e s

      invariant grant_to_node :
        ∀ (e : Sig.Label) (mref : MachineRef),
          e is eGrant → (e targets mref) → (is_Server mref s) → ¬ inflight e s
    }
  }
  Proof {
    prove system_config ;
  }

  Theorem safety {
    system s {
      invariant unique_lock_holder :
        ∀ n1 n2 : Node,
          Node_allocated n1.ref s → Node_allocated n2.ref s →
          (s.machines n1.ref).fields.Node_has_lock = true →
          (s.machines n2.ref).fields.Node_has_lock = true →
          n1 = n2
    }
  }
  Proof {
    prove safety using system_config ;
    prove default using system_config ;
  }

end LockServer

#gen_module LockServer
#pwf        LockServer

set_option pverify.failOnIncomplete false in
#pverify LockServer
