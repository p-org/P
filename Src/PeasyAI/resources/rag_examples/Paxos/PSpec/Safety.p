// Safety Specification: Only one value can be chosen
spec SafetyOnlyOneValueChosen observes eLearn {
    var chosenValue: int;
    var hasChosenValue: bool;

    start state Init {
        entry InitEntry;
    }

    state Monitoring {
        on eLearn do CheckSingleValueChosen;
    }

    fun InitEntry() {
        hasChosenValue = false;
        chosenValue = 0;
        goto Monitoring;
    }

    fun CheckSingleValueChosen(learnMsg: tLearn) {
        if (hasChosenValue) {
            assert chosenValue == learnMsg.finalValue, 
                format("Safety violation: Multiple values chosen. Previously chosen: {0}, Now received: {1}", 
                    chosenValue, learnMsg.finalValue);
        } else {
            chosenValue = learnMsg.finalValue;
            hasChosenValue = true;
        }
    }
}

// Safety Specification: Acceptors only accept proposals with increasing proposal numbers
spec SafetyMonotonicProposalNumbers observes ePromise, eAcceptRequest {
    var acceptorHighestPromised: map[machine, int];
    var acceptorHighestAccepted: map[machine, int];

    start state Init {
        entry InitEntry;
    }

    state Monitoring {
        on ePromise do CheckPromiseMonotonicity;
        on eAcceptRequest do CheckAcceptMonotonicity;
    }

    fun InitEntry() {
        goto Monitoring;
    }

    fun CheckPromiseMonotonicity(promise: tPromise) {
        var acceptor: machine;
        var prevHighest: int;
        
        acceptor = promise.proposer;
        
        if (acceptor in acceptorHighestPromised) {
            prevHighest = acceptorHighestPromised[acceptor];
            assert promise.highestProposalNumber >= prevHighest,
                format("Safety violation: Acceptor promised lower proposal number. Previous: {0}, Current: {1}",
                    prevHighest, promise.highestProposalNumber);
        }
        
        acceptorHighestPromised[acceptor] = promise.highestProposalNumber;
    }

    fun CheckAcceptMonotonicity(acceptReq: tAcceptRequest) {
        var proposer: machine;
        var prevHighest: int;
        
        proposer = acceptReq.proposer;
        
        if (proposer in acceptorHighestAccepted) {
            prevHighest = acceptorHighestAccepted[proposer];
        }
        
        acceptorHighestAccepted[proposer] = acceptReq.proposalNumber;
    }
}

// Safety Specification: Once a value is accepted by a majority, all subsequent acceptances must be for the same value
spec SafetyConsistentAcceptedValues observes eAccepted {
    var acceptedValuesPerProposal: map[int, int];
    var acceptedCountsPerProposal: map[int, int];
    var majorityValue: int;
    var hasMajorityValue: bool;
    var majorityThreshold: int;

    start state Init {
        entry InitEntry;
    }

    state Monitoring {
        on eAccepted do CheckConsistentAcceptedValues;
    }

    fun InitEntry() {
        hasMajorityValue = false;
        majorityValue = 0;
        majorityThreshold = 2;
        goto Monitoring;
    }

    fun CheckConsistentAcceptedValues(accepted: tAccepted) {
        var proposalNum: int;
        var value: int;
        var currentCount: int;
        
        proposalNum = accepted.proposalNumber;
        value = accepted.value;
        
        if (proposalNum in acceptedValuesPerProposal) {
            assert acceptedValuesPerProposal[proposalNum] == value,
                format("Safety violation: Different values accepted for same proposal number {0}. Expected: {1}, Got: {2}",
                    proposalNum, acceptedValuesPerProposal[proposalNum], value);
        } else {
            acceptedValuesPerProposal[proposalNum] = value;
        }
        
        if (proposalNum in acceptedCountsPerProposal) {
            currentCount = acceptedCountsPerProposal[proposalNum];
            acceptedCountsPerProposal[proposalNum] = currentCount + 1;
        } else {
            acceptedCountsPerProposal[proposalNum] = 1;
        }
        
        if (acceptedCountsPerProposal[proposalNum] >= majorityThreshold) {
            if (hasMajorityValue) {
                assert value == majorityValue,
                    format("Safety violation: Different values reached majority. First: {0}, Second: {1}",
                        majorityValue, value);
            } else {
                majorityValue = value;
                hasMajorityValue = true;
            }
        }
    }
}