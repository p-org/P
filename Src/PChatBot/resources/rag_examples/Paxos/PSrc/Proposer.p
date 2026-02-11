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
    }

    state WaitingForClientRequest {
        on eProposeRequest do HandleClientRequest;
    }

    state PreparingProposal {
        on ePromise do HandlePromise;
        on eAcceptRequest goto SendingAcceptRequests;
    }

    state SendingAcceptRequests {
        entry SendAcceptRequestsEntry;
        on eAccepted do HandleAccepted;
        on eLearn goto Finished;
    }

    state Finished {
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
        send learner, eAccepted, (learner = learner, proposalNumber = accepted.proposalNumber, value = accepted.value);
        
        if (acceptsReceived >= majorityThreshold) {
            isProposalActive = false;
        }
    }

    fun BroadcastToAcceptors(eventToSend: event, payload: any) {
        var i: int;
        var acceptor: machine;
        
        i = 0;
        while (i < sizeof(acceptors)) {
            acceptor = acceptors[i];
            send acceptor, eventToSend, payload;
            i = i + 1;
        }
    }
}