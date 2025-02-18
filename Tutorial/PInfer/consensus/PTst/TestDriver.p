fun SetupSystem(num_of_nodes: int) {
    var nodes: set[machine];
    var i: int;
    var n1: machine;
    var n2: machine;
    nodes = default(set[machine]);
    i = 0;
    while (i < num_of_nodes) {
        nodes += (new Node());
        i = i + 1;
    }
    foreach (n1 in nodes) {
        send n1, eConfig, (quorum=num_of_nodes/2+1,);
    }

    foreach (n1 in nodes) {
        foreach (n2 in nodes) {
            send n1, eStart, (requestVoteFrom=n2,);
        }
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

machine FourNodes {
    start state Start {
        entry {
            SetupSystem(4);
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
