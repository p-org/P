machine Acceptor {
    var highestProposalSeen: int;
    var acceptedProposal: int;
    var acceptedValue: int;
    var learners: seq[machine];

    start state Init {
        entry (config: tAcceptorConfig) {
            learners = config.learners;
            highestProposalSeen = -1;
            acceptedProposal = -1;
            acceptedValue = -1;
            goto Ready;
        }
    }

    state Ready {
        on ePropose do (payload: tProposePayload) {
            if (payload.proposalNumber > highestProposalSeen) {
                highestProposalSeen = payload.proposalNumber;
                send payload.proposer, ePromise, (
                    proposer = payload.proposer,
                    highestProposalSeen = highestProposalSeen,
                    acceptedValue = acceptedValue,
                    acceptedProposal = acceptedProposal
                );
            }
        }

        on eAcceptRequest do (payload: tAcceptRequestPayload) {
            if (payload.proposalNumber >= highestProposalSeen) {
                highestProposalSeen = payload.proposalNumber;
                acceptedProposal = payload.proposalNumber;
                acceptedValue = payload.proposedValue;
                NotifyLearners(payload.proposalNumber, payload.proposedValue);
            }
        }
    }

    fun NotifyLearners(propNum: int, propVal: int) {
        var i: int;
        i = 0;
        while (i < sizeof(learners)) {
            send learners[i], eAccepted, (
                proposalNumber = propNum,
                acceptedValue = propVal
            );
            i = i + 1;
        }
    }
}