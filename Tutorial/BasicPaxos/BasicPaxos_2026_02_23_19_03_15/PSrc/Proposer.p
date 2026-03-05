machine Proposer {
    var acceptors: seq[machine];
    var learners: seq[machine];
    var proposalNumber: int;
    var proposedValue: int;
    var promiseCount: int;
    var majoritySize: int;
    var highestAcceptedProposal: int;

    start state Init {
        entry (config: tProposerConfig) {
            acceptors = config.acceptors;
            learners = config.learners;
            proposalNumber = config.proposerId;
            proposedValue = config.valueToPropose;
            majoritySize = sizeof(acceptors) / 2 + 1;
            highestAcceptedProposal = -1;
            goto ProposalPhase;
        }
    }

    state ProposalPhase {
        entry {
            var i: int;
            promiseCount = 0;
            highestAcceptedProposal = -1;
            i = 0;
            while (i < sizeof(acceptors)) {
                send acceptors[i], ePropose, (proposer = this, proposalNumber = proposalNumber, proposedValue = proposedValue);
                i = i + 1;
            }
        }

        on ePromise do (payload: tPromisePayload) {
            if (payload.highestProposalSeen == proposalNumber) {
                if (payload.acceptedProposal > highestAcceptedProposal) {
                    highestAcceptedProposal = payload.acceptedProposal;
                    if (payload.acceptedValue != -1) {
                        proposedValue = payload.acceptedValue;
                    }
                }
                promiseCount = promiseCount + 1;
                if (promiseCount >= majoritySize) {
                    goto AcceptPhase;
                }
            }
        }
    }

    state AcceptPhase {
        entry {
            var i: int;
            i = 0;
            while (i < sizeof(acceptors)) {
                send acceptors[i], eAcceptRequest, (proposer = this, proposalNumber = proposalNumber, proposedValue = proposedValue);
                i = i + 1;
            }
        }

        ignore ePromise;
    }
}

module PaxosModule = { Proposer, Acceptor, Learner };