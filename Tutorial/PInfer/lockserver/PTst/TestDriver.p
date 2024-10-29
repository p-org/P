fun setUpSystem(num_nodes: int) {
    var server: LockServer;
    var i: int;
    i = 0;
    server = new LockServer();
    while (i < num_nodes) {
        new Node((server=server,));
        i = i + 1;
    }
}

machine OneNode {
    start state Init {
        entry {
            setUpSystem(1);
        }
    }
}

machine ThreeNodes {
    start state Init {
        entry {
            setUpSystem(2);
        }
    }
}

machine FourNodes {
    start state Init {
        entry {
            setUpSystem(3);
        }
    }
}

machine FiveNodes {
    start state Init {
        entry {
            setUpSystem(4);
        }
    }
}

test tcOneNode [main = OneNode]:
    assert UniqueGrant, UniqueUnlock, NoGrantForHeldLock in (union { OneNode }, LockServerMod);

test tcThreeNodes [main = ThreeNodes]:
    assert UniqueGrant, UniqueUnlock, NoGrantForHeldLock in (union { ThreeNodes }, LockServerMod);

test tcFourNodes [main = FourNodes]:
    assert UniqueGrant, UniqueUnlock, NoGrantForHeldLock in (union { FourNodes }, LockServerMod);

test tcFiveNodes [main = FiveNodes]:
    assert UniqueGrant, UniqueUnlock, NoGrantForHeldLock in (union { FiveNodes }, LockServerMod);