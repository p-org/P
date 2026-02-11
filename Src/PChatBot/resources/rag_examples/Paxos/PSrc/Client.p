machine Client {
    var proposer: machine;
    var valueToPropose: int;
    var learnedValue: int;
    var hasLearnedValue: bool;

    start state Init {
        entry InitEntry;
    }

    state WaitingForConsensus {
        on eLearn do HandleLearn;
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
    }
}