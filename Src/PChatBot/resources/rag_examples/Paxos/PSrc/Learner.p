machine Learner {
    var acceptedValues: map[int, int];
    var acceptedCounts: map[int, int];
    var majorityThreshold: int;
    var totalAcceptors: int;
    var chosenValue: int;
    var hasChosenValue: bool;
    var allComponents: seq[machine];

    start state Init {
        entry InitEntry;
    }

    state WaitingForAcceptances {
        on eAccepted do HandleAccepted;
    }

    state ValueChosen {
        // Final state after consensus is reached
    }

    fun InitEntry(payload: (acceptors: int, components: seq[machine])) {
        totalAcceptors = payload.acceptors;
        majorityThreshold = (totalAcceptors / 2) + 1;
        hasChosenValue = false;
        chosenValue = 0;
        allComponents = payload.components;
        goto WaitingForAcceptances;
    }

    fun HandleAccepted(acceptance: tAccepted) {
        var proposalNum: int;
        var value: int;
        var currentCount: int;
        
        proposalNum = acceptance.proposalNumber;
        value = acceptance.value;
        
        acceptedValues[proposalNum] = value;
        
        if (proposalNum in acceptedCounts) {
            currentCount = acceptedCounts[proposalNum];
            acceptedCounts[proposalNum] = currentCount + 1;
        } else {
            acceptedCounts[proposalNum] = 1;
        }
        
        if (acceptedCounts[proposalNum] >= majorityThreshold) {
            if (!hasChosenValue) {
                chosenValue = value;
                hasChosenValue = true;
                BroadcastLearn(chosenValue);
                goto ValueChosen;
            }
        }
    }

    fun BroadcastLearn(value: int) {
        var i: int;
        var component: machine;
        
        i = 0;
        while (i < sizeof(allComponents)) {
            component = allComponents[i];
            send component, eLearn, (allComponents = component, finalValue = value);
            i = i + 1;
        }
    }
}