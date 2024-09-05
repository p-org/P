package pex.runtime.scheduler.explicit.choiceselector;

import lombok.Getter;
import lombok.Setter;
import pex.runtime.PExGlobal;
import pex.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pex.utils.random.RandomNumberGenerator;

import java.util.List;

public class ChoiceSelectorQL<S> extends ChoiceSelector {
    private static final double EPSILON_MAX = 1.0;
    private static final double EPSILON_MIN = 0.2;
    @Getter
    private static final ChoiceQL choiceQL = new ChoiceQL();
    @Setter
    private static double EPSILON_DECAY_FACTOR = 0.99999;
    private static double epsilon = EPSILON_MAX;
    private final ChoiceSelector choiceSelectorExplore;

    public ChoiceSelectorQL() {
        choiceSelectorExplore = new ChoiceSelectorRandom();
    }

    protected int select(ExplicitSearchScheduler sch, List<?> choices) {
        S state = (S) (Integer) sch.getChoiceNumber();

        decayEpsilon();
        double randNum = RandomNumberGenerator.getInstance().getRandomDouble();
        int selected = -1;
        if (false) {
            // explore
            selected = choiceSelectorExplore.select(sch, choices);
        } else {
            // exploit
            selected = choiceQL.select(state, choices);
        }

        Object timeline = sch.getStepState().getTimeline();
        if (!PExGlobal.getTimelines().contains(timeline)) {
            // reward new timeline
            choiceQL.rewardNewTimeline(state, choices.get(selected));
        } else {
            choiceQL.penalizeSelected(state, choices.get(selected));
        }

        return selected;
    }

    private void decayEpsilon() {
        if (epsilon > EPSILON_MIN) {
            epsilon *= EPSILON_DECAY_FACTOR;
        } else {
            epsilon = EPSILON_MIN;
        }
    }
}
