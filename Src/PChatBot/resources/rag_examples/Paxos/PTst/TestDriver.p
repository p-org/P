machine TestDriver {
    var numAcceptors: int;
    var numProposers: int;
    var acceptors: seq[machine];
    var proposers: seq[machine];
    var learner: machine;
    var clients: seq[machine];
    var allComponents: seq[machine];

    start state Init {
        entry InitEntry;
    }

    fun InitEntry(config: (acceptors: int, proposers: int, clients: int)) {
        var i: int;
        var acceptor: machine;
        var proposer: machine;
        var client: machine;

        numAcceptors = config.acceptors;
        numProposers = config.proposers;

        // Create acceptors
        i = 0;
        while (i < numAcceptors) {
            acceptor = new Acceptor();
            acceptors += (i, acceptor);
            allComponents += (sizeof(allComponents), acceptor);
            i = i + 1;
        }

        // Create learner
        learner = new Learner((acceptors = numAcceptors, components = allComponents));
        allComponents += (sizeof(allComponents), learner);

        // Create proposers
        i = 0;
        while (i < numProposers) {
            proposer = new Proposer((acceptors = acceptors, learner = learner, totalAcceptors = numAcceptors));
            proposers += (i, proposer);
            allComponents += (sizeof(allComponents), proposer);
            i = i + 1;
        }

        // Create clients
        i = 0;
        while (i < config.clients) {
            if (i < numProposers) {
                client = new Client((proposer = proposers[i], value = i + 100));
                clients += (i, client);
                allComponents += (sizeof(allComponents), client);
            }
            i = i + 1;
        }
    }
}

machine TestDriverScenario1 {
    start state Init {
        entry {
            var driver: machine;
            driver = new TestDriver((acceptors = 3, proposers = 1, clients = 1));
        }
    }
}

machine TestDriverScenario2 {
    start state Init {
        entry {
            var driver: machine;
            driver = new TestDriver((acceptors = 5, proposers = 2, clients = 2));
        }
    }
}

machine TestDriverScenario3 {
    start state Init {
        entry {
            var driver: machine;
            driver = new TestDriver((acceptors = 5, proposers = 2, clients = 2));
        }
    }
}