// Proposer machine for Paxos protocol.
// Initiates proposals and drives consensus with acceptors.
//
// BEST PRACTICE: Every state should handle or ignore events that may arrive
// from broadcast messages (e.g., eLearn broadcast by Learner).
machine Proposer {
    var currentProposalNumber: int;
    var proposedValue: int;
    var promisesReceived: int;
    var acceptsReceived: int;
    var majorityThreshold: int;
    var totalAcceptors: int;
    var acceptors: seq[machine];
    var learner: machine;
    var client: machine;
    var highestAcceptedProposalNumber: int;
    var highestAcceptedValue: int;
    var hasHighestAcceptedValue: bool;
    var isProposalActive: bool;

    start state Init {
        entry InitEntry;
        ignore eLearn;
    }

    state WaitingForClientRequest {
        on eProposeRequest do HandleClientRequest;
        ignore eLearn;
    }

    state PreparingProposal {
        on ePromise do HandlePromise;
        on eAcceptRequest goto SendingAcceptRequests;
        ignore eLearn;
    }

    state SendingAcceptRequests {
        entry SendAcceptRequestsEntry;
        on eAccepted do HandleAccepted;
        // BEST PRACTICE: Use goto to transition when consensus is reached.
        on eLearn goto Finished;
    }

    state Finished {
        // BEST PRACTICE: In terminal states, ignore all events that may still arrive.
        ignore ePromise, eAccepted, eLearn, eProposeRequest;
    }

    fun InitEntry(payload: (acceptors: seq[machine], learner: machine, totalAcceptors: int)) {
        acceptors = payload.acceptors;
        learner = payload.learner;
        totalAcceptors = payload.totalAcceptors;
        majorityThreshold = (totalAcceptors / 2) + 1;
        currentProposalNumber = 0;
        promisesReceived = 0;
        acceptsReceived = 0;
        highestAcceptedProposalNumber = -1;
        highestAcceptedValue = 0;
        hasHighestAcceptedValue = false;
        isProposalActive = false;
        goto WaitingForClientRequest;
    }

    fun HandleClientRequest(request: tProposeRequest) {
        var i: int;
        var acceptor: machine;
        
        client = request.client;
        proposedValue = request.value;
        currentProposalNumber = currentProposalNumber + 1;
        promisesReceived = 0;
        acceptsReceived = 0;
        hasHighestAcceptedValue = false;
        highestAcceptedProposalNumber = -1;
        isProposalActive = true;
        
        i = 0;
        while (i < sizeof(acceptors)) {
            acceptor = acceptors[i];
            send acceptor, ePropose, (proposer = this, proposalNumber = currentProposalNumber, value = proposedValue);
            i = i + 1;
        }
        
        goto PreparingProposal;
    }

    fun HandlePromise(promise: tPromise) {
        promisesReceived = promisesReceived + 1;
        
        if (promise.hasAcceptedValue) {
            if (promise.highestProposalNumber > highestAcceptedProposalNumber) {
                highestAcceptedProposalNumber = promise.highestProposalNumber;
                highestAcceptedValue = promise.acceptedValue;
                hasHighestAcceptedValue = true;
            }
        }
        
        if (promisesReceived >= majorityThreshold) {
            raise eAcceptRequest, (proposer = this, proposalNumber = currentProposalNumber, value = proposedValue);
        }
    }

    fun SendAcceptRequestsEntry() {
        var valueToPropose: int;
        var i: int;
        var acceptor: machine;
        
        if (hasHighestAcceptedValue) {
            valueToPropose = highestAcceptedValue;
        } else {
            valueToPropose = proposedValue;
        }
        
        acceptsReceived = 0;
        
        i = 0;
        while (i < sizeof(acceptors)) {
            acceptor = acceptors[i];
            send acceptor, eAcceptRequest, (proposer = this, proposalNumber = currentProposalNumber, value = valueToPropose);
            i = i + 1;
        }
    }

    fun HandleAccepted(accepted: tAccepted) {
        acceptsReceived = acceptsReceived + 1;
        
        if (acceptsReceived >= majorityThreshold) {
            isProposalActive = false;
        }
    }
}
