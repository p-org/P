package pexplicit.runtime.scheduler.explicit.choiceselector;

import pexplicit.utils.random.RandomNumberGenerator;

import java.util.List;

public class ChoiceSelectorRandom extends ChoiceSelector {

    public ChoiceSelectorRandom() {
    }

    public int selectChoice(List<?> choices) {
        return RandomNumberGenerator.getInstance().getRandomInt(choices.size());
    }
}
