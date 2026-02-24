spec OnlyOneValueChosen observes eLearn {
    var chosenValue: int;
    var hasChosenValue: bool;

    start state Init {
        entry {
            hasChosenValue = false;
            chosenValue = 0;
            goto Monitoring;
        }
    }

    state Monitoring {
        on eLearn do CheckSingleValueChosen;
    }

    fun CheckSingleValueChosen(payload: tLearnPayload) {
        if (hasChosenValue) {
            assert chosenValue == payload.learnedValue,
                format("Safety violation: Multiple values chosen. Previously chosen: {0}, Now received: {1}",
                    chosenValue, payload.learnedValue);
        } else {
            chosenValue = payload.learnedValue;
            hasChosenValue = true;
        }
    }
}