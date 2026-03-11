type tPaxosTestConfig = (numProposers: int, numAcceptors: int, numLearners: int);

fun SetupPaxos(config: tPaxosTestConfig) {
    var i: int;
    var acceptors: seq[machine];
    var learners: seq[machine];
    var majoritySize: int;

    majoritySize = config.numAcceptors / 2 + 1;

    i = 0;
    while (i < config.numLearners) {
        learners += (sizeof(learners), new Learner((majoritySize = majoritySize,)));
        i = i + 1;
    }

    i = 0;
    while (i < config.numAcceptors) {
        acceptors += (sizeof(acceptors), new Acceptor((learners = learners,)));
        i = i + 1;
    }

    i = 0;
    while (i < config.numProposers) {
        new Proposer((acceptors = acceptors, learners = learners, proposerId = i + 1, valueToPropose = (i + 1) * 100));
        i = i + 1;
    }
}

machine TestBasicConsensus {
    start state Init {
        entry {
            SetupPaxos((numProposers = 1, numAcceptors = 3, numLearners = 1));
        }
    }
}

machine TestMultipleProposers {
    start state Init {
        entry {
            SetupPaxos((numProposers = 2, numAcceptors = 5, numLearners = 1));
        }
    }
}

machine TestThreeProposers {
    start state Init {
        entry {
            SetupPaxos((numProposers = 3, numAcceptors = 3, numLearners = 1));
        }
    }
}

test testBasicConsensus [main = TestBasicConsensus]:
    assert OnlyOneValueChosen in (union PaxosModule, { TestBasicConsensus });

test testMultipleProposers [main = TestMultipleProposers]:
    assert OnlyOneValueChosen in (union PaxosModule, { TestMultipleProposers });

test testThreeProposers [main = TestThreeProposers]:
    assert OnlyOneValueChosen in (union PaxosModule, { TestThreeProposers });