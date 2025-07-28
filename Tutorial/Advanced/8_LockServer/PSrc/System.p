event eLock: (sender: Node);
event eUnlock: (sender: Node);
event eGrant;

event eAquire;
event eRelease;

pure lock_server(): machine;

init-condition forall (m: machine) :: m is Server == (m == lock_server());
init-condition forall (n: Node) :: n.server == lock_server() && !n.has_lock;
init-condition forall (m: Server) :: m.has_lock;

machine Server {
    var has_lock: bool;

    start state Serving {
        on eLock do (p: (sender: Node)) {
            if (has_lock) {
                has_lock = false;
                send p.sender, eGrant;
            }
        }

        on eUnlock do (p: (sender: Node)) {
            has_lock = true;
        }
    }
}

machine Node {
    var has_lock: bool;
    var server: machine;

    start state Working {

        on eAquire do {
            send server, eLock, (sender=this,);
        }

        on eRelease do {
            if (has_lock) {
                has_lock = false;
                send server, eUnlock, (sender=this,);
            }
        }

        on eGrant do {
            has_lock = true;
        }
    }
}

Lemma system_config {
    invariant aquire_to_node: forall (e: eAquire, m: machine) :: e targets m && m is Server ==> !inflight e;
    invariant release_to_node: forall (e: eRelease, m: machine) :: e targets m && m is Server ==>!inflight e;
    invariant grant_to_node: forall (e: eGrant, m: machine) :: e targets m && m is Server ==> !inflight e;
    invariant node_send_lock: forall (e: eLock) :: inflight e ==> e.sender is Node;
    invariant node_send_unlock: forall (e: eUnlock) :: inflight e ==> e.sender is Node;

    invariant lock_to_server: forall (e: eLock, m: Node) :: e targets m ==> !inflight e;
    invariant unlock_to_server: forall (e: eUnlock, m: Node) :: e targets m ==> !inflight e;

    invariant const_server: forall (m: machine) :: m is Server == (m == lock_server());
    invariant unique_server: forall (m1: machine, m2: machine) :: 
        m1 is Server && m2 is Server ==> m1 == m2;
    invariant const_server_ref: forall (n: Node) :: n.server == lock_server();
}
Proof {
    prove system_config;
}

Theorem safety {
    invariant unique_lock_holder:
        forall (n1: Node, n2: Node) :: n1.has_lock && n2.has_lock ==> n1 == n2;

    // Mutually relative inductive lemmas
    invariant unique_grant:
        forall (e1: eGrant, e2: eGrant) :: inflight e1 && inflight e2 ==> e1 == e2;
    invariant unique_unlock:
        forall (e1: eUnlock, e2: eUnlock) :: inflight e1 && inflight e2 ==> e1 == e2;
    invariant grant_server_unlocked:
        forall (e: eGrant, m: Server) :: inflight e ==> !m.has_lock;
    invariant no_lock_while_grant:
        forall (e: eGrant, n: Node) :: inflight e ==> !n.has_lock;
    invariant no_lock_while_unlock:
        forall (e: eUnlock, n: Node, s: Server) :: inflight e ==> !n.has_lock && !s.has_lock;
    invariant grant_not_unlock:
        forall (e1: eGrant, e2: eUnlock) :: !(inflight e1 && inflight e2);
    invariant node_server_mutex:
        forall (n: Node, s: Server) :: !(n.has_lock && s.has_lock);
}
Proof {
    prove safety using system_config;
    prove default using system_config;
}