// Scenario 1: 3 acceptors, 1 proposer, 1 learner
machine Scenario1_BasicConsensus {
    start state Init {
        entry {
            var learner: machine;
            var learners: seq[machine];
            var acceptors: seq[machine];
            var proposer: machine;
            var i: int;

            learner = new Learner((majoritySize = 2,));
            learners += (0, learner);

            i = 0;
            while (i < 3) {
                acceptors += (i, new Acceptor((learners = learners,)));
                i = i + 1;
            }

            proposer = new Proposer((acceptors = acceptors, learners = learners, proposerId = 1, valueToPropose = 100));
            send proposer, eStartConsensus, 100;
        }
    }
}

// Scenario 2: 5 acceptors, 2 proposers, 1 learner (competing proposals)
machine Scenario2_CompetingProposals {
    start state Init {
        entry {
            var learner: machine;
            var learners: seq[machine];
            var acceptors: seq[machine];
            var proposer1: machine;
            var proposer2: machine;
            var i: int;

            learner = new Learner((majoritySize = 3,));
            learners += (0, learner);

            i = 0;
            while (i < 5) {
                acceptors += (i, new Acceptor((learners = learners,)));
                i = i + 1;
            }

            proposer1 = new Proposer((acceptors = acceptors, learners = learners, proposerId = 1, valueToPropose = 200));
            proposer2 = new Proposer((acceptors = acceptors, learners = learners, proposerId = 2, valueToPropose = 300));

            send proposer1, eStartConsensus, 200;
            send proposer2, eStartConsensus, 300;
        }
    }
}

test tcBasicConsensus [main=Scenario1_BasicConsensus]:
    assert OnlyOneValueChosen in
    { Proposer, Acceptor, Learner, Scenario1_BasicConsensus };

test tcCompetingProposals [main=Scenario2_CompetingProposals]:
    assert OnlyOneValueChosen in
    { Proposer, Acceptor, Learner, Scenario2_CompetingProposals };