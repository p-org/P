type tEpoch = int;
type tRound = int;
type tGrant = (node: Node, epoch: tEpoch);
type tTransfer = (round: tRound, node: Node, epoch: tEpoch);

event eGrant: tGrant;
event eTransfer: tTransfer;


// helper events
event eHasLock: (round: tRound, node: Node, epoch: tEpoch);
event eTransfered: tTransfer;
event eNodeState: (round: tRound, node: Node, hasLock: bool, epoch: tEpoch);
event eSynchronize;

event eConfig: (peers: set[Node]);

machine Node {
    var epoch: tEpoch;
    var syncRound: tRound;
    var hasLock: bool;
    var peers: set[Node];

    start state Init {
        entry (init: (hasLock: bool)) {
            epoch = 0;
            syncRound = 0;
            hasLock = init.hasLock;
        }

        on eConfig do (e: (peers: set[Node])) {
            peers = e.peers;
            goto Serving;
        }
        defer eGrant, eTransfer, eSynchronize;
    }

    state Serving {
        entry {
            var p: Node;
            var ep: tEpoch;
            if (!hasLock) {
                ep = epoch + choose(20);
                foreach (p in peers) {
                    if (p != this) {
                        send p, eGrant, (node=this, epoch=ep);
                    }
                }
            }
        }

        on eGrant do (pld: tGrant) {
            var n: Node;
            n = pld.node;
            if (hasLock && epoch < pld.epoch) {
                hasLock = false;
                send n, eTransfer, (round=syncRound, node=n, epoch=pld.epoch);
            }
        }

        on eSynchronize do {
            announce eNodeState, (round=syncRound, node=this, hasLock=hasLock, epoch=epoch);
            syncRound = syncRound + 1;
        }

        on eTransfer do (pld: tTransfer) {
            var m: Node;
            if (pld.epoch > epoch) {
                hasLock = true;
                announce eHasLock, (round=syncRound, node=this, epoch=pld.epoch);
                epoch = pld.epoch;
                // send pld.node, eGrant, (node=this, epoch=epoch);
                announce eNodeState, (round=syncRound, node=this, hasLock=hasLock, epoch=epoch);
                syncRound = syncRound + 1;
                foreach (m in peers) {
                    if (m != this) {
                        send m, eSynchronize;
                    }
                }
            }
        }
    }
}

spec Safety observes eHasLock {
    var idx: map[tEpoch, Node];
    start state Init {
        entry {
            idx = default(map[tEpoch, Node]);
        }

        on eHasLock do (pld: (round: tRound, node: Node, epoch: tEpoch)) {
            if (pld.epoch in keys(idx)) {
                assert idx[pld.epoch] == pld.node;
            }
            idx[pld.epoch] = pld.node;
        }
    }
}

module DistributedLockMod = { Node };