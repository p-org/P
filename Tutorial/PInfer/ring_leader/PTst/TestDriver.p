fun SetupSystem(n: int) {
    var nodes: seq[machine];
    var i: int;
    var node: machine;
    nodes = default(seq[machine]);
    i = 0;
    while (i < n) {
        nodes += (sizeof(nodes), new Node((ballot=choose(100),)));
        i = i + 1;
    }
    i = 0;
    while (i < n) {
        send nodes[i], eConfig, (nxt=nodes[(i + 1) % n],);
        i = i + 1;
    }
    i = 0;
    while (i < n) {
        if ($) {
            send nodes[i], eStart;
            break;
        }
        i = i + 1;
    }
}

machine OneNode {
    start state Start {
        entry {
            SetupSystem(1);
        }
    }
}

machine TwoNodes {
    start state Start {
        entry {
            SetupSystem(2);
        }
    }
}

machine ThreeNodes {
    start state Start {
        entry {
            SetupSystem(3);
        }
    }
}

machine FiveNodes {
    start state Start {
        entry {
            SetupSystem(5);
        }
    }
}

machine TenNodes {
    start state Start {
        entry {
            SetupSystem(10);
        }
    }
}
