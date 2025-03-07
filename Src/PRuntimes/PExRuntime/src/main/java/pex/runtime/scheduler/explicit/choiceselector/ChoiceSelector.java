package pex.runtime.scheduler.explicit.choiceselector;

import pex.runtime.scheduler.explicit.ExplicitSearchScheduler;

import java.io.Serializable;
import java.util.List;

public abstract class ChoiceSelector implements Serializable {
    protected abstract int select(ExplicitSearchScheduler sch, List<?> choices);

    public int selectChoice(ExplicitSearchScheduler sch, List<?> choices) {
        if (choices.size() == 1) {
            return 0;
        } else {
            return select(sch, choices);
        }
    }
}
