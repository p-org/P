// Client machine for Paxos protocol.
// Initiates requests and waits for consensus to be reached.
machine Client {
    var proposer: machine;
    var valueToPropose: int;
    var learnedValue: int;
    var hasLearnedValue: bool;

    start state Init {
        entry InitEntry;
        ignore eLearn;
    }

    state WaitingForConsensus {
        on eLearn do HandleLearn;
    }

    state Done {
        // BEST PRACTICE: In terminal states, ignore events that may still arrive.
        ignore eLearn;
    }

    fun InitEntry(payload: (proposer: machine, value: int)) {
        proposer = payload.proposer;
        valueToPropose = payload.value;
        learnedValue = 0;
        hasLearnedValue = false;
        send proposer, eProposeRequest, (client = this, value = valueToPropose);
        goto WaitingForConsensus;
    }

    fun HandleLearn(learnMsg: tLearn) {
        learnedValue = learnMsg.finalValue;
        hasLearnedValue = true;
        goto Done;
    }
}
