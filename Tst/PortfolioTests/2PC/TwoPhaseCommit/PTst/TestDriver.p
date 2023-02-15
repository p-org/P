// type that represents the configuration of the system under test
type t2PCConfig = (
  numClients: int,
  numParticipants: int,
  numTransPerClient: int,
  failParticipants: int,
  chooseFrom: int
);

// function that creates the two phase commit system along with the machines in its
// environment (test harness)
fun SetUpTwoPhaseCommitSystem(config: t2PCConfig)
{
  var coordinator : Coordinator;
  var participants: set[Participant];
  var i : int;

  // create participants
  while (i < config.numParticipants) {
    participants += (new Participant());
    i = i + 1;
  }

  // initialize the monitors (specifications)
  InitializeTwoPhaseCommitSpecifications(config.numParticipants);

  // create the coordinator
  coordinator = new Coordinator(participants);

  // create the clients
  i = 0;
  while(i < config.numClients)
  {
    new Client((coordinator = coordinator, n = config.numTransPerClient, id = i + 1, chooseFrom = config.chooseFrom));
    i = i + 1;
  }

  // create the failure injector if we want to inject failures
  if(config.failParticipants > 0)
  {
    CreateFailureInjector((nodes = participants, nFailures = config.failParticipants));
  }
}

fun InitializeTwoPhaseCommitSpecifications(numParticipants: int) {
  // inform the monitor the number of participants in the system
  announce eMonitor_AtomicityInitialize, numParticipants;
}

// original
/*
This machine creates the 3 participants, 1 coordinator, and 1 clients
*/
machine SingleClientNoFailure {
  start state Init {
    entry {
      var config: t2PCConfig;

      config = (numClients = 1,
                      numParticipants = 3,
                      numTransPerClient = 2,
                      failParticipants = 0,
                      chooseFrom = 2);

            SetUpTwoPhaseCommitSystem(config);
    }
  }
}

/*
This machine creates the 3 participants, 1 coordinator, and 1 clients
*/
machine MultipleClientsNoFailure {
  start state Init {
    entry {
      var config: t2PCConfig;
      config = 
        (numClients = 2,
        numParticipants = 3,
        numTransPerClient = 2,
        failParticipants = 0,
        chooseFrom = 2);

        SetUpTwoPhaseCommitSystem(config);
    }
  }
}

/*
This machine creates the 3 participants, 1 coordinator, 1 Failure injector, and 2 clients
*/
machine MultipleClientsWithFailure {
  start state Init {
    entry {
      var config: t2PCConfig;
      config = 
        (numClients = 2,
        numParticipants = 3,
        numTransPerClient = 2,
        failParticipants = 1,
        chooseFrom = 2);

      SetUpTwoPhaseCommitSystem(config);
    }
  }
}


// more data
/*
This machine creates the 3 participants, 1 coordinator, and 1 clients
*/
machine SingleClientNoFailureMoreData {
  start state Init {
    entry {
      var config: t2PCConfig;

      config = (numClients = 1,
                      numParticipants = 3,
                      numTransPerClient = 2,
                      failParticipants = 0,
                      chooseFrom = 4);

            SetUpTwoPhaseCommitSystem(config);
    }
  }
}

/*
This machine creates the 3 participants, 1 coordinator, and 1 clients
*/
machine MultipleClientsNoFailureMoreData {
  start state Init {
    entry {
      var config: t2PCConfig;
      config =
        (numClients = 2,
        numParticipants = 3,
        numTransPerClient = 2,
        failParticipants = 0,
        chooseFrom = 4);

        SetUpTwoPhaseCommitSystem(config);
    }
  }
}

/*
This machine creates the 3 participants, 1 coordinator, 1 Failure injector, and 2 clients
*/
machine MultipleClientsWithFailureMoreData {
  start state Init {
    entry {
      var config: t2PCConfig;
      config =
        (numClients = 2,
        numParticipants = 3,
        numTransPerClient = 2,
        failParticipants = 1,
        chooseFrom = 4);

      SetUpTwoPhaseCommitSystem(config);
    }
  }
}


// more participants
/*
This machine creates the 6 participants, 1 coordinator, and 1 clients
*/
machine SingleClientNoFailureMoreParticipants {
  start state Init {
    entry {
      var config: t2PCConfig;

      config = (numClients = 1,
                      numParticipants = 6,
                      numTransPerClient = 2,
                      failParticipants = 0,
                      chooseFrom = 2);

            SetUpTwoPhaseCommitSystem(config);
    }
  }
}

/*
This machine creates the 6 participants, 1 coordinator, and 1 clients
*/
machine MultipleClientsNoFailureMoreParticipants {
  start state Init {
    entry {
      var config: t2PCConfig;
      config =
        (numClients = 2,
        numParticipants = 6,
        numTransPerClient = 2,
        failParticipants = 0,
        chooseFrom = 2);

        SetUpTwoPhaseCommitSystem(config);
    }
  }
}

/*
This machine creates the 6 participants, 1 coordinator, 1 Failure injector, and 2 clients
*/
machine MultipleClientsWithFailureMoreParticipants {
  start state Init {
    entry {
      var config: t2PCConfig;
      config =
        (numClients = 2,
        numParticipants = 6,
        numTransPerClient = 2,
        failParticipants = 1,
        chooseFrom = 2);

      SetUpTwoPhaseCommitSystem(config);
    }
  }
}
