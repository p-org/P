// Test Driver for Paxos Protocol.
//
// BEST PRACTICES demonstrated:
// 1. Use a parameterized TestDriver to avoid code duplication across scenarios.
// 2. Create machines in dependency order: Learner -> Acceptors -> Proposers -> Clients.
// 3. Use setup events (eSetupLearnerComponents) for post-creation initialization
//    when there are circular dependencies.
// 4. NEVER misuse protocol events (like eLearn) for initialization/setup.
// 5. Build complete component lists AFTER all machines are created.

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

        // STEP 1: Create the Learner first (with just the acceptor count).
        // We can't pass allComponents yet because Proposers/Clients don't exist.
        learner = new Learner((acceptors = numAcceptors,));
        allComponents += (sizeof(allComponents), learner);

        // STEP 2: Create Acceptors (they need the Learner reference).
        i = 0;
        while (i < numAcceptors) {
            acceptor = new Acceptor((learnerSet = default(seq[machine]),));
            acceptors += (i, acceptor);
            allComponents += (sizeof(allComponents), acceptor);
            i = i + 1;
        }

        // STEP 3: Create Proposers (they need Acceptor list and Learner).
        i = 0;
        while (i < numProposers) {
            proposer = new Proposer((acceptors = acceptors, learner = learner, totalAcceptors = numAcceptors));
            proposers += (i, proposer);
            allComponents += (sizeof(allComponents), proposer);
            i = i + 1;
        }

        // STEP 4: Create Clients (they need a Proposer).
        i = 0;
        while (i < config.clients) {
            if (i < numProposers) {
                client = new Client((proposer = proposers[i], value = i + 100));
                clients += (i, client);
                allComponents += (sizeof(allComponents), client);
            }
            i = i + 1;
        }

        // STEP 5: BEST PRACTICE — Use setup event for post-creation initialization.
        // Now that ALL components are created, send the complete list to the Learner.
        // This avoids the circular dependency problem.
        send learner, eSetupLearnerComponents, allComponents;
    }
}

// Scenario machines delegate to the parameterized TestDriver.
// BEST PRACTICE: Keep scenario machines minimal — just configuration.

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

// BEST PRACTICE: Test declarations should include the safety specs.
test testPaxosScenario1 [main=TestDriverScenario1]:
    assert SafetyOnlyOneValueChosen in
    { Proposer, Acceptor, Learner, Client, TestDriver, TestDriverScenario1 };

test testPaxosScenario2 [main=TestDriverScenario2]:
    assert SafetyOnlyOneValueChosen in
    { Proposer, Acceptor, Learner, Client, TestDriver, TestDriverScenario2 };
