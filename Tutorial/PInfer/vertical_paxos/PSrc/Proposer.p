machine Proposer {
    var round: tRound;
    var completed: tRound;
    var value: tValue;
    var acceptors: seq[machine];
    var learners: seq[machine];
    var promises: map[tRound, set[machine]];
    var votes: set[machine];
    var maxr: tRound;
    var orchestrator: Orchestrator;

    start state Init {
        on eConfig do (c: tConfig) {
            round = c.round;
            completed = c.completed;
            acceptors = c.acceptors;
            learners = c.learners;
            promises = default(map[tRound, set[machine]]);
            votes = default(set[machine]);
            maxr = -10;
            value = -1;
            orchestrator = c.view;
            goto Proposing;
        }
        ignore eP2B, eP1B;
    }

    state Proposing {
        entry {
            var r: tRound;
            var a: machine;
            r = completed;
            while (r < round) {
                foreach (a in acceptors) {
                    send a, eP1A, (proposer=this, round=round, rp=r);
                }
                r = r + 1;
            }
        }

        on eP1B do (p: tP1B) {
            var a: Acceptor;
            if (p.round == round) {
                if (!(p.maxr in keys(promises))) {
                    promises[p.maxr] = default(set[machine]);
                }
                promises[p.maxr] += (p.acceptor);
                if (p.maxr > maxr && p.v != -1) {
                    maxr = p.maxr;
                    value = p.v;
                }
                if (quorum_voted(promises, completed, round)) {
                    goto Accepting;
                }
            }
        }
        ignore eConfig, eP2B;
    }

    state Accepting {
        entry {
            var a: machine;
            var r: tRound;
            if (value == -1) {
                // round completed
                send orchestrator, eRoundCompleteOnPropose, (round=round,);
                value = choose(100);
            }
            r = completed;
            while (r < round) {
                announce ePromised, (prev_round=r,);
                r = r + 1;
            }
            foreach (a in acceptors) {
                send a, eP2A, (proposer=this, round=round, completed=completed, value=value);
            }
        }

        on eP2B do (p: tP2B) {
            votes += (p.acceptor);
            if (sizeof(votes) >= sizeof(acceptors) / 2 + 1) {
                goto Decided;
            }
        }
        ignore eConfig, eP1B;
    }

    state Decided {
        entry {
            var l: machine;
            foreach (l in learners) {
                send l, eDecided, (round=round, value=value);
            }
            send orchestrator, eRoundCompleteOnDecide, (round=round,);
            goto Init;
        }
        ignore eConfig, eP1B, eP2B;
    }

    fun quorum_voted(v: map[tRound, set[machine]], cr: tRound, r: tRound): bool {
        var i: tRound;
        var ok: bool;
        i = cr;
        ok = true;
        while (i < r) {
            if (!(i in keys(v))) {
                ok = false;
                break;
            }
            if (sizeof(v[i]) < sizeof(acceptors) / 2 + 1) {
                ok = false;
                break;
            }
            i = i + 1;
        }
        return ok;
    }
}