package pexplicit.runtime.scheduler.explicit.choiceselector;

import pexplicit.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pexplicit.utils.random.RandomNumberGenerator;

import java.util.List;

public class ChoiceSelectorRandom extends ChoiceSelector {

    public ChoiceSelectorRandom() {
    }

    public int select(ExplicitSearchScheduler sch, List<?> choices) {
        return RandomNumberGenerator.getInstance().getRandomInt(choices.size());
    }
}
