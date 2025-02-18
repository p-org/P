type LockId = int;

event eLock: (node: Node);
event eUnlock: (node: Node, epoch: LockId);
event eGrant: (node: Node, epoch: LockId);
event eHoldsLock: (node: Node, epoch: LockId);
event eServerState: (holdsLock: bool, epoch: LockId); 

event eNotifySelf;

machine LockServer {
    var holdsLock: bool;
    var epoch: LockId;

    start state WaitForLock {
        entry {
            holdsLock = true;
            epoch = 0;
        }

        on eLock do (e: (node: Node)) {
            announce eServerState, (holdsLock=holdsLock, epoch=epoch);
            if (holdsLock) {
                holdsLock = false;
                send e.node, eGrant, (node=e.node, epoch=epoch);
            }
        }

        on eUnlock do (e: (node: Node, epoch: LockId)) {
            announce eServerState, (holdsLock=holdsLock, epoch=epoch);
            if (!holdsLock && epoch == e.epoch) {
                holdsLock = true;
                epoch = e.epoch + 1;
            }
        }
    }
}

machine Node {
    var epoch: LockId;
    var holdsLock: bool;
    var server: LockServer;
    start state Init {
        entry (setup: (server: LockServer)) {
            server = setup.server;
            epoch = -1;
            holdsLock = false;
            send server, eLock, (node=this,);
        }

        on eGrant do (e: (node: Node, epoch: LockId)) {
            if (!holdsLock) {
                epoch = e.epoch;
                holdsLock = true;
                announce eHoldsLock, (node=this, epoch=epoch);
                send this, eNotifySelf;
            }
        }

        on eNotifySelf do {
            send server, eUnlock, (node=this, epoch=epoch);
        }
    }
}

spec UniqueGrant observes eGrant {
    var grants: map[LockId, Node];
    start state Monitor {
        entry {
            grants = default(map[LockId, Node]);
        }

        on eGrant do (e: (node: Node, epoch: LockId)) {
            if (e.epoch in keys(grants)) {
                assert grants[e.epoch] == e.node;
            } else {
                grants[e.epoch] = e.node;
            }
        }
    }
}

spec NoGrantForHeldLock observes eGrant, eHoldsLock {
    var holdsLock: map[Node, LockId];
    start state Monitor {
        entry {
            holdsLock = default(map[Node, LockId]);
        }

        on eGrant do (e: (node: Node, epoch: LockId)) {
            if (e.node in keys(holdsLock)) {
                assert holdsLock[e.node] != e.epoch;
            }
        }

        on eHoldsLock do (e: (node: Node, epoch: LockId)) {
            holdsLock[e.node] = e.epoch;
        }
    }
}

spec UniqueUnlock observes eUnlock {
    var unlocks: map[LockId, Node];
    start state Monitor {
        entry {
            unlocks = default(map[LockId, Node]);
        }

        on eUnlock do (e: (node: Node, epoch: LockId)) {
            if (e.epoch in keys(unlocks)) {
                assert unlocks[e.epoch] == e.node;
            } else {
                unlocks[e.epoch] = e.node;
            }
        }
    }
}

module LockServerMod = {LockServer, Node};