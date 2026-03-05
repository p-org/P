machine Acceptor {
    var highestProposalSeen: int;
    var acceptedProposal: int;
    var acceptedValue: int;
    var learners: seq[machine];

    start state Init {
        entry InitEntry;
    }

    state Ready {
        on ePropose do HandlePropose;
        on eAcceptRequest do HandleAcceptRequest;
        ignore eStartConsensus;
    }

    fun InitEntry(config: tAcceptorConfig) {
        learners = config.learners;
        highestProposalSeen = -1;
        acceptedProposal = -1;
        acceptedValue = -1;
        goto Ready;
    }

    fun HandlePropose(msg: tProposePayload) {
        if (msg.proposalNumber > highestProposalSeen) {
            highestProposalSeen = msg.proposalNumber;
            send msg.proposer, ePromise, (proposer = this, highestProposalSeen = highestProposalSeen, acceptedValue = acceptedValue, acceptedProposal = acceptedProposal);
        }
    }

    fun HandleAcceptRequest(msg: tAcceptRequestPayload) {
        var i: int;
        if (msg.proposalNumber >= highestProposalSeen) {
            acceptedProposal = msg.proposalNumber;
            acceptedValue = msg.proposedValue;
            highestProposalSeen = msg.proposalNumber;
            i = 0;
            while (i < sizeof(learners)) {
                send learners[i], eAccepted, (proposalNumber = msg.proposalNumber, acceptedValue = msg.proposedValue);
                i = i + 1;
            }
        }
    }
}