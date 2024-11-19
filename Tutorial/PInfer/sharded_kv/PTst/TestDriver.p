fun nextValue(vset: set[ValueT]): ValueT {
    var v: ValueT;
    v = 0;
    while (v in vset) {
        v = v + choose(10);
    }
    return v;
}

fun setupExperiment(num_nodes: int, num_keys: int, num_reshards: int) {
    var ownerships: seq[map[KeyT, ValueT]];
    var nodesToKV: map[Node, map[KeyT, ValueT]];
    var nodes: set[Node];
    var reshardedKeys: set[KeyT];
    var i: int;
    var j: int;
    var n: Node;
    var k: KeyT;
    var v: ValueT;
    var keySet: set[KeyT];
    var valueSet: set[ValueT];

    var src: Node;
    var dst: Node;
    ownerships = default(seq[map[KeyT, ValueT]]);
    nodesToKV = default(map[Node, map[KeyT, ValueT]]);
    reshardedKeys = default(set[KeyT]);
    keySet = default(set[KeyT]);
    valueSet = default(set[ValueT]);
    i = 0;
    while (i < num_nodes) {
        ownerships += (i, default(map[KeyT, ValueT]));
        i = i + 1;
    }
    i = 0;
    while (i < num_keys) {
        j = 0;
        while (i < num_keys && j < sizeof(ownerships)) {
            // round-robin assignment of keys to nodes
            k = nextValue(keySet);
            v = nextValue(valueSet);
            keySet += (k);
            valueSet += (v);
            ownerships[j][k] = v;
            j = j + 1;
            i = i + 1;
        }
    }
    i = 0;
    while (i < num_nodes) {
        nodes += (new Node((kvstore=ownerships[i],)));
        nodesToKV[nodes[i]] = ownerships[i];
        i = i + 1;
    }
    i = 0;
    while (i < num_reshards && sizeof(reshardedKeys) < num_keys) {
        src = choose(nodes);
        dst = choose(nodes);
        if (src == dst || sizeof(nodesToKV[src]) == 0) {
            continue;
        }
        k = choose(keys(nodesToKV[src]));
        if (k in reshardedKeys) {
            continue;
        }
        reshardedKeys += (k);
        send src, eReshard, (key=k, value=nodesToKV[src][k], dst=dst);
        nodesToKV[dst][k] = nodesToKV[src][k];
        nodesToKV[src] -= (k);
        i = i + 1;
    }
    foreach (n in nodes) {
        send n, eReport;
    }
}

machine TwoNodes {
    start state Init {
        entry {
            setupExperiment(2, 5, 3);
        }
    }
}

machine ThreeNodes {
    start state Init {
        entry {
            setupExperiment(3, 5, 3);
        }
    }
}

machine FourNodes {
    start state Init {
        entry {
            setupExperiment(4, 10, 3);
        }
    }
}

machine FiveNodes {
    start state Init {
        entry {
            setupExperiment(5, 15, 3);
        }
    }
}

test tcTwoNodes [main=TwoNodes]:
    assert Safety in (union { TwoNodes }, ShardedKV);

test tcThreeNodes [main=ThreeNodes]:
    assert Safety in (union { ThreeNodes }, ShardedKV);

test tcFourNodes [main=FourNodes]:
    assert Safety in (union { FourNodes }, ShardedKV);

test tcFiveNodes [main=FiveNodes]:
    assert Safety in (union { FiveNodes }, ShardedKV);