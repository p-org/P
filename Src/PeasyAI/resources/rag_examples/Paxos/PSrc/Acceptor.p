// Acceptor machine for Paxos protocol.
// Participates in voting on proposals and forwards acceptances to learners.
//
// BEST PRACTICE: Handle or ignore ALL events that may be broadcast to this machine.
// Since the Learner broadcasts eLearn to all components, we must ignore it here.
machine Acceptor {
    var highestProposalNumberSeen: int;
    var acceptedProposalNumber: int;
    var acceptedValue: int;
    var hasAcceptedValue: bool;
    var learners: seq[machine];

    start state Init {
        entry InitEntry;
        // BEST PRACTICE: Ignore events that may arrive before initialization completes.
        ignore eLearn;
    }

    state WaitingForProposals {
        on ePropose do HandlePropose;
        on eAcceptRequest do HandleAcceptRequest;
        // BEST PRACTICE: When a Learner broadcasts eLearn to all components,
        // the Acceptor should ignore it since it doesn't need to act on consensus results.
        ignore eLearn;
    }

    // BEST PRACTICE: Use a payload tuple to pass initialization parameters.
    // This avoids the need for setup events in simple cases.
    fun InitEntry(payload: (learnerSet: seq[machine],)) {
        highestProposalNumberSeen = -1;
        acceptedProposalNumber = -1;
        acceptedValue = 0;
        hasAcceptedValue = false;
        learners = payload.learnerSet;
        goto WaitingForProposals;
    }

    fun HandlePropose(proposal: tProposal) {
        if (proposal.proposalNumber > highestProposalNumberSeen) {
            highestProposalNumberSeen = proposal.proposalNumber;
            send proposal.proposer, ePromise, (proposer = proposal.proposer, highestProposalNumber = highestProposalNumberSeen, acceptedValue = acceptedValue, hasAcceptedValue = hasAcceptedValue);
        }
    }

    fun HandleAcceptRequest(request: tAcceptRequest) {
        var i: int;
        
        if (request.proposalNumber >= highestProposalNumberSeen) {
            highestProposalNumberSeen = request.proposalNumber;
            acceptedProposalNumber = request.proposalNumber;
            acceptedValue = request.value;
            hasAcceptedValue = true;
            
            // BEST PRACTICE: Forward acceptance to ALL learners.
            // In Paxos, acceptors notify learners of accepted values directly.
            i = 0;
            while (i < sizeof(learners)) {
                send learners[i], eAccepted, (learner = learners[i], proposalNumber = acceptedProposalNumber, value = acceptedValue);
                i = i + 1;
            }
        }
    }
}
