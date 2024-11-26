machine SingleClientTest {
    start state Init {
        entry {
            testSetup(1, 3, 5, 1, 10);
        }
    }
}

machine SingleClientMultipleKeys {
    start state Init {
        entry {
            testSetup(1, 3, 5, 3, 10);
        }
    }
}

machine MultipleClientSingleKey {
    start state Init {
        entry {
            testSetup(3, 4, 5, 1, 10);
        }
    }
}

machine MultipleClientMultipleKeys {
    start state Init {
        entry {
            testSetup(3, 4, 5, 3, 10);
        }
    }
}

// a P function for setting up the test environment
fun testSetup(clientsCount: int, replicatesCount: int, numOps: int, keyBound: int, valueBound: int) {
    var clients: set[Client];
    var replicates: seq[Replicate];
    var failureInjector: FailureInjector;
    var i: int;

    i = 0;
    while(i < replicatesCount) {
        replicates += (i, new Replicate());
        i = i + 1;
    }
    i = 0;
    while(i < clientsCount) {
        clients += (new Client((numOps = numOps, keyBound = keyBound, valueBound = valueBound)));
        i = i + 1;
    }
    // create the failure injector
    failureInjector = new FailureInjector((nodes = seqToSet(replicates) as set[Replicate], nFailures = sizeof(replicates) - 1));
    new Master((failureInjector = failureInjector, replicates = replicates, clients = clients));
}