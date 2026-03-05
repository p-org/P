machine Learner {
    var acceptCount: map[int, int];
    var acceptedValues: map[int, int];
    var majoritySize: int;
    var chosenValue: int;
    var hasLearned: bool;

    start state Init {
        entry (config: tLearnerConfig) {
            majoritySize = config.majoritySize;
            chosenValue = -1;
            hasLearned = false;
            goto Learning;
        }
    }

    state Learning {
        on eAccepted do (payload: tAcceptedPayload) {
            if (!hasLearned) {
                if (!(payload.proposalNumber in acceptCount)) {
                    acceptCount[payload.proposalNumber] = 0;
                    acceptedValues[payload.proposalNumber] = payload.acceptedValue;
                }
                acceptCount[payload.proposalNumber] = acceptCount[payload.proposalNumber] + 1;

                if (acceptCount[payload.proposalNumber] >= majoritySize) {
                    chosenValue = acceptedValues[payload.proposalNumber];
                    hasLearned = true;
                    announce eLearn, (learnedValue = chosenValue,);
                }
            }
        }
    }
}