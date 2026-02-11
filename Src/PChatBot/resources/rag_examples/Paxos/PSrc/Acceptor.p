machine Acceptor {
    var highestProposalNumberSeen: int;
    var acceptedProposalNumber: int;
    var acceptedValue: int;
    var hasAcceptedValue: bool;

    start state Init {
        entry InitEntry;
    }

    state WaitingForProposals {
        on ePropose do HandlePropose;
        on eAcceptRequest do HandleAcceptRequest;
    }

    fun InitEntry() {
        highestProposalNumberSeen = -1;
        acceptedProposalNumber = -1;
        acceptedValue = 0;
        hasAcceptedValue = false;
        goto WaitingForProposals;
    }

    fun HandlePropose(proposal: tProposal) {
        if (proposal.proposalNumber > highestProposalNumberSeen) {
            highestProposalNumberSeen = proposal.proposalNumber;
            send proposal.proposer, ePromise, (proposer = proposal.proposer, highestProposalNumber = highestProposalNumberSeen, acceptedValue = acceptedValue, hasAcceptedValue = hasAcceptedValue);
        }
    }

    fun HandleAcceptRequest(request: tAcceptRequest) {
        if (request.proposalNumber >= highestProposalNumberSeen) {
            highestProposalNumberSeen = request.proposalNumber;
            acceptedProposalNumber = request.proposalNumber;
            acceptedValue = request.value;
            hasAcceptedValue = true;
        }
    }
}