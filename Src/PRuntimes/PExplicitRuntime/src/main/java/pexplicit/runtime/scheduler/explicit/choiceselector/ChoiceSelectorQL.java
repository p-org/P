package pexplicit.runtime.scheduler.explicit.choiceselector;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pexplicit.runtime.scheduler.explicit.StatefulBacktrackingMode;
import pexplicit.runtime.scheduler.explicit.StepState;
import pexplicit.utils.random.RandomNumberGenerator;

import java.util.List;

public class ChoiceSelectorQL extends ChoiceSelector {
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
        int state = 0;
        if (PExplicitGlobal.getConfig().getStatefulBacktrackingMode() != StatefulBacktrackingMode.None) {
            StepState stepState = sch.getSchedule().getStepBeginState();
            if (stepState != null) {
                state = stepState.getTimelineHash();
            }
        }

        decayEpsilon();
        double randNum = RandomNumberGenerator.getInstance().getRandomDouble();
        int selected = -1;
        if (randNum <= epsilon) {
            // explore
            selected = choiceSelectorExplore.select(sch, choices);
        } else {
            // exploit
            selected = choiceQL.select(state, choices);
        }
        choiceQL.penalizeSelected(state, choices.get(selected));
        return selected;
    }

    public void rewardNewTimeline(ExplicitSearchScheduler sch) {
        choiceQL.rewardScheduleChoices(sch);
    }

    private void decayEpsilon() {
        if (epsilon > EPSILON_MIN) {
            epsilon *= EPSILON_DECAY_FACTOR;
        } else {
            epsilon = EPSILON_MIN;
        }
    }
}
