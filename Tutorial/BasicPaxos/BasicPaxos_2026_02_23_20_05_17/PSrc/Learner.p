machine Learner {
    var acceptedValues: map[int, int];
    var acceptCount: map[int, int];
    var majoritySize: int;
    var chosenValue: int;
    var hasLearned: bool;

    start state Init {
        entry InitEntry;
    }

    state Learning {
        on eAccepted do HandleAccepted;
        ignore ePropose, ePromise, eAcceptRequest, eStartConsensus;
    }

    fun InitEntry(config: tLearnerConfig) {
        majoritySize = config.majoritySize;
        acceptedValues = default(map[int, int]);
        acceptCount = default(map[int, int]);
        chosenValue = -1;
        hasLearned = false;
        goto Learning;
    }

    fun HandleAccepted(msg: tAcceptedPayload) {
        var propNum: int;
        var accValue: int;
        var currentCount: int;

        propNum = msg.proposalNumber;
        accValue = msg.acceptedValue;

        acceptedValues[propNum] = accValue;

        if (propNum in acceptCount) {
            currentCount = acceptCount[propNum];
            currentCount = currentCount + 1;
            acceptCount[propNum] = currentCount;
        } else {
            acceptCount[propNum] = 1;
            currentCount = 1;
        }

        if (currentCount >= majoritySize && !hasLearned) {
            chosenValue = accValue;
            hasLearned = true;
            announce eLearn, (learnedValue = chosenValue,);
        }
    }
}