machine Orchestrator {
    var r: tRound;
    var completed: tRound;
    var acceptors: seq[machine];
    var learners: seq[machine];
    var proposers: seq[machine];

    start state Init {
        entry (cfg: (p: int, a: int, l: int)) {
            r = 0;
            completed = -1;
            acceptors = default(seq[machine]);
            learners = default(seq[machine]);
            proposers = default(seq[machine]);
            while (sizeof(acceptors) < cfg.a) {
                acceptors += (sizeof(acceptors), new Acceptor());
            }
            while (sizeof(learners) < cfg.l) {
                learners += (sizeof(learners), new Learner());
            }
            while (sizeof(proposers) < cfg.p) {
                proposers += (sizeof(proposers), new Proposer());
            }
            announce ePaxosConfig, (quorum=cfg.a / 2 + 1,);
            goto Running;
        }
    }

    state Running {
        entry {
            var p: machine;
            p = choose(proposers);
            send p, eConfig, (view=this, round=r, completed=completed, acceptors=acceptors, learners=learners);
            r = r + 1;
        }

        on eRoundCompleteOnPropose do (p: tRoundComplete) {
            if (p.round > completed) {
                completed = p.round;
            }
        }

        on eRoundCompleteOnDecide do (p: tRoundComplete) {
            if (p.round > completed) {
                completed = p.round;
            }
        }

        on eReconfig do {
            goto Running;
        }
    }
}