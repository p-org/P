machine Learner {
    var value: tValue;

    start state Learning {
        entry {
            value = -1;
        }
        on eDecided do (p: tDecided) {
            if (value == -1) {
                value = p.value;
            }
            assert p.value == value;
        }
    }
}

spec OneValueDecided observes eDecided {
    var value: tValue;
    start state Init {
        entry {
            value = -1;
        }
        on eDecided do (p: tDecided) {
            if (value == -1) {
                value = p.value;
            }
            assert p.value == value;
        }
    }
}