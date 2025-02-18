fun SetupSystem(n: int) {
    var nodes: seq[machine];
    var i: int;
    var node: machine;
    nodes = default(seq[machine]);
    i = 0;
    while (i < n) {
        nodes += (sizeof(nodes), new Node());
        i = i + 1;
    }
    i = 0;
    while (i < n) {
        send nodes[i], eNodeConfig, (ballot=choose(100), right=nodes[(i+1)%n]);
        i = i + 1;
    }
    send nodes[0], eStart;
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
