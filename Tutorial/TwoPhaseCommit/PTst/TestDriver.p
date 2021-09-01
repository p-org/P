
/*
This machine creates the 2 participants, 1 coordinator, and 2 clients 
*/
type t2PCConfig = (
    numClients: int,
    numParticipants: int,
    numTransPerClient: int,
    failParticipants: int
);

fun SetUpTwoPhaseCommitSystem(config: t2PCConfig)
{
    var coordinator : Coordinator;
    var participants: set[Participant];
    var i : int;

    i = 0;
    while (i < config.numParticipants) {
        participants += (new Participant());
        i = i + 1;
    }

    coordinator = new Coordinator(participants);

    i = 0;
    while(i < config.numClients)
    {
        new Client((coordinator = coordinator, n = config.numTransPerClient));
        i = i + 1;
    }

    if(config.failParticipants > 0)
    {
        new FailureInjector((participants = participants, nFailures = config.failParticipants));
    }
}

fun InitializeTwoPhaseCommitSpecifications(numParticipants: int) {
    // inform the monitor the number of participants in the system
    announce eMonitor_AtomicityInitialize, numParticipants;
}

machine TestDriverNoFailure {
	start state Init {
		entry {
			var config: t2PCConfig;

			config = (numClients = 2,
                      numParticipants = 3,
                      numTransPerClient = 2,
                      failParticipants = 0);

            SetUpTwoPhaseCommitSystem(config);
		}
	}
}

/*
This machine creates the 2 participants, 1 coordinator, 1 Failure injector, and 2 clients 
*/
machine TestDriverWithFailure {
	start state Init {
		entry {
			var config: t2PCConfig;

            config = (numClients = 2,
                      numParticipants = 3,
                      numTransPerClient = 2,
                      failParticipants = 1);

            SetUpTwoPhaseCommitSystem(config);
		}
	}
}

module ClientAndFailureInjector = { Client, FailureInjector };
