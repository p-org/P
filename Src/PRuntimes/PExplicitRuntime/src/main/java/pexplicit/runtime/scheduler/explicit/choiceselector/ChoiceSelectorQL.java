package pexplicit.runtime.scheduler.explicit.choiceselector;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pexplicit.utils.random.RandomNumberGenerator;

import java.util.List;

public class ChoiceSelectorQL extends ChoiceSelector {
    private static final double EPSILON_MAX = 1.0;
    private static final double EPSILON_MIN = 0.2;
    @Getter
    private static final ChoiceQL choiceQL = new ChoiceQL();
    @Setter
    private static double EPSILON_DECAY_FACTOR = 0.999;
    private static double epsilon = EPSILON_MAX;
    private final ChoiceSelector choiceSelectorExplore;

    public ChoiceSelectorQL() {
        choiceSelectorExplore = new ChoiceSelectorRandom();
    }

    public int selectChoice(List<?> choices) {
        decayEpsilon();
        double randNum = RandomNumberGenerator.getInstance().getRandomDouble();
        int selected = -1;
        if (randNum <= epsilon) {
            // explore
            selected = choiceSelectorExplore.selectChoice(choices);
        } else {
            // exploit
            selected = choiceQL.selectChoice(choices);
        }
        choiceQL.addChoice(choices.get(selected));
        return selected;
    }

    @Override
    public void startStep(ExplicitSearchScheduler sch) {
        choiceQL.startStep(sch);
    }

    private void decayEpsilon() {
        if (epsilon > EPSILON_MIN) {
            epsilon *= EPSILON_DECAY_FACTOR;
        } else {
            epsilon = EPSILON_MIN;
        }
    }
}
