type KeyT = int;
type ValueT = int;

type tTransfer = (src: Node, key: KeyT, value: ValueT, dst: Node);
type tReshard = (key: KeyT, value: ValueT, dst: Node);

event eTransfer: tTransfer;
event eReshard: tReshard;
event eReport;


event eOwns: (node: Node, key: KeyT, value: ValueT);

machine Node {
    var kv: map[KeyT, ValueT];
    
    start state Init {
        entry (setup: (kvstore: map[KeyT, ValueT])) {
            kv = setup.kvstore;
        }

        on eReshard do (e: tReshard) {
            if (e.key in kv) {
                send e.dst, eTransfer, (src=this, key=e.key, value=kv[e.key], dst=e.dst);
                kv -= (e.key);
            }
        }

        on eTransfer do (e: tTransfer) {
            var k: KeyT;
            if (e.dst != this) {
                return;
            }
            foreach (k in keys(kv)) {
                if (k != e.key) {
                    continue;
                } else {
                    assert false;
                }
            }
            kv[e.key] = e.value;
            announce eOwns, (node=this, key=e.key, value=e.value);
        }

        on eReport do {
            var k: KeyT;
            foreach (k in keys(kv)) {
                announce eOwns, (node=this, key=k, value=kv[k]);
            }
        }
    }
}

spec Safety observes eOwns {
    var ownerships: map[KeyT, Node];
    start state Init {
        entry {
            ownerships = default(map[KeyT, Node]);
        }

        on eOwns do (e: (node: Node, key: KeyT, value: ValueT)) {
            if (e.key in keys(ownerships)) {
                assert(ownerships[e.key] == e.node);
            } else {
                ownerships[e.key] = e.node;
            }
        }
    }
}

module ShardedKV = {Node};