fun SetupSystem (num_nodes: int) {
    var nodes: set[Node];
    var i: int;
    var lockIssued: bool;
    var n: Node;

    nodes = default(set[Node]);
    lockIssued = false;
    i = 0;
    while (i < num_nodes) {
        if (!lockIssued) {
            nodes += (new Node((hasLock=true,)));
            lockIssued = true;
        } else {
            nodes += (new Node((hasLock=false,)));
        }
        i = i + 1;
    }
    i = 0;
    foreach (n in nodes) {
        send n, eConfig, (peers=nodes,);
    }
}

// hint exact Test(e0: eNodeState, e1: eGrant) {}

machine ThreeNodes {
    start state Init {
        entry {
            SetupSystem(3);
        }
    }
}

machine FourNodes {
    start state Init {
        entry {
            SetupSystem(4);
        }
    }
}

machine FiveNodes {
    start state Init {
        entry {
            SetupSystem(5);
        }
    }
}

machine SixNodes {
    start state Init {
        entry {
            SetupSystem(6);
        }
    }
}

test tcThreeNodes [main = ThreeNodes]:
    assert Safety in (union { ThreeNodes }, DistributedLockMod);

test tcFourNodes [main = FourNodes]:
    assert Safety in (union { FourNodes }, DistributedLockMod);

test tcFiveNodes [main = FiveNodes]:
    assert Safety in (union { FiveNodes }, DistributedLockMod);

test tcSixNodes [main = SixNodes]:
    assert Safety in (union { SixNodes }, DistributedLockMod);
