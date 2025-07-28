type tKey = int;
type tValue = int;

type tTransfer = (source: Node, key: tKey, value: tValue);
type tReshard = (reshard_key: tKey, reshard_to: Node);

event eTransfer: tTransfer;
event eReshard: tReshard;

pure owns(n: Node, k: tKey): bool = k in n.kv;

machine Node {
    var kv: map[tKey, tValue];

    start state Serving {
        on eReshard do (e: tReshard) {
            var v: tValue;
            if (e.reshard_key in kv) {
                v = kv[e.reshard_key];
                kv -= (e.reshard_key);
                send e.reshard_to, eTransfer, (source=this, key=e.reshard_key, value=v);
            }
        }

        on eTransfer do (e: tTransfer) {
            var k: tKey;
            var v: tValue;
            kv[e.key] = e.value;
        }
    }
}

Theorem Safety {        
    invariant transfer_means_no_owner:
        forall (e1: eTransfer, n: Node) ::
            inflight e1 ==> !owns(n, e1.key);
    invariant unique_key_transfer:
        forall (e1: eTransfer, e2: eTransfer) :: 
            inflight e1 && inflight e2 && e1.key == e2.key ==> e1 == e2;
    invariant transfer_means_not_own:
        forall (e: eTransfer) :: inflight e ==> !owns(e.source, e.key);
    invariant unique_owner:
        forall (k: tKey, n1: Node, n2: Node) ::
            owns(n1, k) && owns(n2, k) ==> n1 == n2;
}
Proof of_Safety {
    prove Safety;
    prove default;
}