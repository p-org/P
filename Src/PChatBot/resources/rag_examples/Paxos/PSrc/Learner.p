// Learner machine for Paxos protocol.
// Learns the chosen value once a majority of acceptors agree.
//
// BEST PRACTICE: For circular dependency resolution (Learner needs allComponents
// but components need Learner), use a setup event to pass the component list
// AFTER all machines are created. See eSetupLearnerComponents below.
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
        // BEST PRACTICE: Accept a setup event for post-creation initialization.
        // This solves circular dependency: Learner needs to know about all components,
        // but components (Acceptors, Proposers) need the Learner reference first.
        on eSetupLearnerComponents do HandleSetupComponents;
        ignore eLearn;
    }

    state WaitingForAcceptances {
        on eAccepted do HandleAccepted;
        // BEST PRACTICE: Also accept setup event in this state in case it
        // arrives after we've transitioned (due to event ordering).
        on eSetupLearnerComponents do HandleSetupComponents;
        ignore eLearn;
    }

    state ValueChosen {
        // Terminal state after consensus is reached.
        // BEST PRACTICE: Ignore events that may still be in flight.
        ignore eAccepted, eLearn, eSetupLearnerComponents;
    }

    fun InitEntry(payload: (acceptors: int,)) {
        totalAcceptors = payload.acceptors;
        majorityThreshold = (totalAcceptors / 2) + 1;
        hasChosenValue = false;
        chosenValue = 0;
        goto WaitingForAcceptances;
    }

    // BEST PRACTICE: Use a dedicated setup event handler instead of misusing
    // protocol events (like eLearn) for initialization.
    fun HandleSetupComponents(components: seq[machine]) {
        allComponents = components;
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
