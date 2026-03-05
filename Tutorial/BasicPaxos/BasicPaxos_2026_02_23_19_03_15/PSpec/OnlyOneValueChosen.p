spec OnlyOneValueChosen observes eLearn {
    var chosenValue: int;

    start state Init {
        entry {
            chosenValue = -1;
        }

        on eLearn do (payload: tLearnPayload) {
            assert(payload.learnedValue != -1);
            if (chosenValue != -1) {
                assert chosenValue == payload.learnedValue,
                    format("Safety violation: previously chose {0} but now learning {1}",
                        chosenValue, payload.learnedValue);
            }
            chosenValue = payload.learnedValue;
        }
    }
}