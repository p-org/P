package pexplicit.runtime.scheduler.explicit.choiceselector;

import pexplicit.runtime.scheduler.explicit.ExplicitSearchScheduler;

import java.io.Serializable;
import java.util.List;

public abstract class ChoiceSelector implements Serializable {
    public abstract int selectChoice(List<?> choices);

    public void startStep(ExplicitSearchScheduler sch) {
    }
}
