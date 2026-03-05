machine Proposer {
    var acceptors: seq[machine];
    var learners: seq[machine];
    var proposerId: int;
    var valueToPropose: int;
    var proposalNumber: int;
    var proposedValue: int;
    var promiseCount: int;
    var majoritySize: int;
    var highestAcceptedProposal: int;
    var highestAcceptedValue: int;

    start state Init {
        entry InitEntry;
        on eStartConsensus goto ProposalPhase with StartProposal;
    }

    state ProposalPhase {
        entry ProposalPhaseEntry;
        on ePromise do HandlePromise;
        defer eStartConsensus;
        ignore eAcceptRequest;
    }

    state AcceptPhase {
        entry AcceptPhaseEntry;
        defer ePromise;
        defer eStartConsensus;
        ignore eAcceptRequest;
    }

    fun InitEntry(config: tProposerConfig) {
        acceptors = config.acceptors;
        learners = config.learners;
        proposerId = config.proposerId;
        valueToPropose = config.valueToPropose;
        majoritySize = sizeof(acceptors) / 2 + 1;
        proposalNumber = proposerId;
        highestAcceptedProposal = -1;
        highestAcceptedValue = -1;
        promiseCount = 0;
    }

    fun StartProposal(value: int) {
        proposedValue = value;
        promiseCount = 0;
        highestAcceptedProposal = -1;
        highestAcceptedValue = -1;
    }

    fun ProposalPhaseEntry() {
        var i: int;
        var payload: tProposePayload;
        
        payload = (proposer = this, proposalNumber = proposalNumber, proposedValue = proposedValue);
        
        i = 0;
        while (i < sizeof(acceptors)) {
            send acceptors[i], ePropose, payload;
            i = i + 1;
        }
    }

    fun HandlePromise(msg: tPromisePayload) {
        promiseCount = promiseCount + 1;
        
        if (msg.acceptedProposal > highestAcceptedProposal) {
            highestAcceptedProposal = msg.acceptedProposal;
            highestAcceptedValue = msg.acceptedValue;
        }
        
        if (promiseCount >= majoritySize) {
            goto AcceptPhase;
        }
    }

    fun AcceptPhaseEntry() {
        var i: int;
        var valueToAccept: int;
        var payload: tAcceptRequestPayload;
        
        if (highestAcceptedValue != -1) {
            valueToAccept = highestAcceptedValue;
        } else {
            valueToAccept = proposedValue;
        }
        
        payload = (proposer = this, proposalNumber = proposalNumber, proposedValue = valueToAccept);
        
        i = 0;
        while (i < sizeof(acceptors)) {
            send acceptors[i], eAcceptRequest, payload;
            i = i + 1;
        }
    }
}