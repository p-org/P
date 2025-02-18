type tBallot = int;
event eNominate: (ballot: tBallot);
event eBecomeLeader: (ballot: tBallot);

event eConfig: (nxt: machine);
event eStart;

machine Node {
    var ballot: tBallot;
    var nxt: machine;

    start state Init {
        entry (cfg: (ballot: tBallot)) {
            ballot = cfg.ballot;
        }

        on eConfig do (cfg: (nxt: machine)) {
            nxt = cfg.nxt;
            goto Nominating;
        }
    }

    state Nominating {
        on eStart do {
            send nxt, eNominate, (ballot=ballot,);
        }

        on eNominate do (n: (ballot: tBallot)) {
            if (n.ballot == ballot) {
                announce eBecomeLeader, (ballot=ballot,);
            } else if (n.ballot < ballot) {
                send nxt, eNominate, (ballot=ballot,);
            } else {
                send nxt, eNominate, (ballot=n.ballot,);
            }
        }
    }
}

spec Safety observes eBecomeLeader {
    var ballot: tBallot;

    start state Monitoring {
        entry {
            ballot = -1;
        }

        on eBecomeLeader do (b: (ballot: tBallot)) {
            if (ballot == -1) {
                ballot = b.ballot;
            }
            assert b.ballot == ballot;
        }
    }
}

module RingLeader = { Node };