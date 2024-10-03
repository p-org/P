type tBallot = int;
event eNominate: (ballot: tBallot);
event eBecomeLeader: (ballot: tBallot);

type tNodeConfig = (ballot: tBallot, right: machine);
event eNodeConfig: tNodeConfig;

event eStart;

machine Node {
    var ballot: tBallot;
    var right: machine;

    start state Init {
        on eNodeConfig do (c: tNodeConfig) {
            ballot = c.ballot;
            right = c.right;
            goto Nominating;
        }
    }

    state Nominating {
        on eStart do {
            send right, eNominate, (ballot=ballot,);
        }

        on eNominate do (n: (ballot: tBallot)) {
            if (n.ballot == ballot) {
                announce eBecomeLeader, (ballot=ballot,);
            } else if (n.ballot < ballot) {
                send right, eNominate, (ballot=ballot,);
            } else {
                send right, eNominate, (ballot=n.ballot,);
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