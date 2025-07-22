event eGrant: (node: Node, epoch: int);
event eAccept: (epoch: int, source: Node);

machine Node {
    var epoch: int;
    var held: bool;

    start state Act {
        on eGrant do (payload: (node: Node, epoch: int)) {
            if (held && payload.epoch > epoch) {
                held = false;
                send payload.node, eAccept, (epoch=payload.epoch, source=this);
            }
        }

        on eAccept do (payload: (epoch: int, source: Node)) {
            if (payload.epoch > epoch) {
                held = true;
                epoch = payload.epoch;
            }
        }
    }
}

init-condition exists (n: Node) :: n.held && n.epoch > 0 && forall (n1: Node) :: n1 != n ==> !n1.held && n1.epoch == 0;

Theorem safety {
    invariant unique_holder: forall (n1: Node, n2: Node) :: n1.held && n2.held ==> n1 == n2;
    invariant no_lock_while_transfer:
        forall (n: Node, e: eAccept) :: inflight e ==> !n.held;
    invariant unique_accept:
        forall (e1: eAccept, e2: eAccept) :: inflight e1 && inflight e2 ==> e1 == e2;
    invariant not_held_after_release:
        forall (n1: Node, e: eAccept) :: inflight e && e.source == n1 ==> !n1.held;
    invariant transfer_to_higher:
        forall (n1: Node, n2: Node, e: eAccept) :: inflight e && e.source == n1 && e targets n2 ==> e.epoch > n1.epoch;
}
Proof Safety {
    prove safety;
    prove default;
}