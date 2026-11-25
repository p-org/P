/-
PLean port of [`Tutorial/Advanced/8_LockServer`](../../../Tutorial/Advanced/8_LockServer/PSrc/System.p).

Exercises:
- two machine kinds (`Server`, `Node`) with `m is Server` / `m is Node`,
- multi-lemma `using` chain (`prove safety using system_config`),
- a `pure lock_server() : machine` modelled as an opaque constant.

Both verification blocks port the full invariant set from the P source.
`system_config` carries the topology lemmas (every Server is
`lock_server`, every Node points at it, and where each event may /
may not be in flight); `safety` carries `unique_lock_holder` together
with the mutually-inductive strengthening invariants
(`unique_grant`, `no_lock_while_grant`, `node_server_mutex`, …) that
make it close.
-/
import PLean

open PLean Std.WP

-- Each obligation now preserves the full invariant bundle (10 in
-- `system_config`, 8 in `safety`), so the per-obligation VC is far
-- larger than the single-invariant skeleton; the 3 s probe budget that
-- the skeleton used returns `unknown` on these. 30 s matches the
-- DistributedLock port.

pmodule LockServer

  system s

  type tLockSender   = (sender : PLean.MachineRef)
  type tUnlockSender = (sender : PLean.MachineRef)

  event eLock    : tLockSender
  event eUnlock  : tUnlockSender
  event eGrant
  event eAquire
  event eRelease

  -- P source has `pure lock_server() : machine`; modelled as an
  -- opaque constant (no body).
  function lock_server : Server

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
    var server   : Server

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

  init-holds ∀ n : Node, n.server = lock_server
  init-holds ∀ n : Node, n.has_lock = false
  init-holds ∀ sv : Server, sv = lock_server
  init-holds is_Server lock_server s

  -- Topology lemma: every Server is `lock_server`, every Node points at
  -- it, and the routing facts that say where each event may/may not be
  -- in flight. Ported in full from the P source's `system_config`.
  -- `node_send_lock` / `node_send_unlock` are the load-bearing additions
  -- the earlier skeleton dropped: the Server's `eLock` handler forwards a
  -- fresh `eGrant` to the lock's `sender`, so preserving `grant_to_node`
  -- needs to know that sender is a Node (hence not a Server).
  Lemma system_config {
    invariant const_server : ∀ n : Node, n.server = lock_server
    invariant serv_is_serv : is_Server lock_server s
    invariant single_server : ∀ sv : Server, sv = lock_server
    invariant unique_server :
      ∀ (m1 m2 : MachineRef),
        is_Server m1 s → is_Server m2 s → m1 = m2

    invariant aquire_to_node :
      ∀ (e : PLean.Msg E) (mref : MachineRef),
        e is eAquire → (e targets mref) → (is_Server mref s) → ¬ inflight e s

    invariant release_to_node :
      ∀ (e : PLean.Msg E) (mref : MachineRef),
        e is eRelease → (e targets mref) → (is_Server mref s) → ¬ inflight e s

    invariant grant_to_node :
      ∀ (e : PLean.Msg E) (mref : MachineRef),
        e is eGrant → (e targets mref) → (is_Server mref s) → ¬ inflight e s

    invariant lock_to_server :
      ∀ (e : PLean.Msg E) (mref : MachineRef),
        e is eLock → (e targets mref) → (is_Node mref s) → ¬ inflight e s

    invariant unlock_to_server :
      ∀ (e : PLean.Msg E) (mref : MachineRef),
        e is eUnlock → (e targets mref) → (is_Node mref s) → ¬ inflight e s

    invariant node_send_lock :
      ∀ e : eLock, inflight e s → is_Node (e.sender) s

    invariant node_send_unlock :
      ∀ e : eUnlock, inflight e s → is_Node (e.sender) s
  }
  Proof {
    prove system_config ;
  }

  -- Safety: at most one Node holds the lock. `unique_lock_holder` is not
  -- inductive on its own; it closes only together with the mutually-
  -- inductive strengthening invariants ported from the P source — the
  -- uniqueness of in-flight grants/unlocks, the "no Node/Server holds the
  -- lock while a grant/unlock is in flight" facts, and the Node/Server
  -- mutex. Each is dropped by the earlier skeleton.
  Theorem safety {
    invariant unique_lock_holder :
      ∀ n1 n2 : Node,
        n1.has_lock = true →
        n2.has_lock = true →
        n1 = n2

    invariant unique_grant :
      ∀ e1 e2 : eGrant, inflight e1 s → inflight e2 s → e1 = e2

    invariant unique_unlock :
      ∀ e1 e2 : eUnlock, inflight e1 s → inflight e2 s → e1 = e2

    invariant grant_server_unlocked :
      ∀ (e : eGrant) (sv : Server), inflight e s → sv.has_lock = false

    invariant no_lock_while_grant :
      ∀ (e : eGrant) (n : Node), inflight e s → n.has_lock = false

    invariant no_lock_while_unlock :
      ∀ (e : eUnlock) (n : Node) (sv : Server),
        inflight e s → n.has_lock = false ∧ sv.has_lock = false

    invariant grant_not_unlock :
      ∀ (e1 : eGrant) (e2 : eUnlock), ¬ (inflight e1 s ∧ inflight e2 s)

    invariant node_server_mutex :
      ∀ (n : Node) (sv : Server), ¬ (n.has_lock = true ∧ sv.has_lock = true)
  }
  Proof {
    prove safety using system_config ;
  }

end LockServer

#gen_module LockServer
#pwf        LockServer

#pverify    LockServer
