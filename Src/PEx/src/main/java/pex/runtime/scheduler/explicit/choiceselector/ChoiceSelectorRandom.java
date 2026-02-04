package pex.runtime.scheduler.explicit.choiceselector;

import pex.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pex.utils.random.RandomNumberGenerator;

import java.util.List;

public class ChoiceSelectorRandom extends ChoiceSelector {

    public ChoiceSelectorRandom() {
    }

    public int select(ExplicitSearchScheduler sch, List<?> choices) {
        return RandomNumberGenerator.getInstance().getRandomInt(choices.size());
    }
}
