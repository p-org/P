package pex.runtime.scheduler.explicit.choiceselector;

import lombok.Getter;
import pex.runtime.logger.PExLogger;
import pex.utils.random.RandomNumberGenerator;

import java.io.Serializable;
import java.util.List;

public class ChoiceQL<S> implements Serializable {
    @Getter
    private static final double defaultQValue = 1.0;
    private static final double ALPHA = 0.3;
    private static final double GAMMA = 0.2;
    private static final double STEP_PENALTY_REWARD = -1.0;
    private static final double NEW_TIMELINE_REWARD = 1.0;
    private final ChoiceQTable<S, Object> qValues;

    public ChoiceQL() {
        qValues = new ChoiceQTable();
    }

    private void rewardAction(S state, Object action, double reward) {
        ChoiceQTable.ChoiceQStateEntry stateEntry = qValues.get(state);
        ChoiceQTable.ChoiceQClassEntry classEntry = stateEntry.get(action.getClass());
        double maxQ = classEntry.getMaxQ();
        double oldVal = classEntry.get(action);
        double newVal = ((1 - ALPHA) * oldVal + ALPHA * (reward + GAMMA * maxQ));
        classEntry.update(action, newVal);
    }

    public int select(S state, List<?> choices) {
        // Compute the total and minimum weight
        double totalWeight = 0.0;
        double minWeight = Double.MAX_VALUE;
        for (int i = 0; i < choices.size(); i++) {
            Object choice = choices.get(i);
            double weight = qValues.get(state, choice.getClass(), choice);
            totalWeight += weight;
            if (weight < minWeight) {
                minWeight = weight;
            }
        }

        // Now choose a weighted random item
        int idx = 0;
        for (double r = RandomNumberGenerator.getInstance().getRandomDouble() * totalWeight; idx < choices.size() - 1; idx++) {
            Object choice = choices.get(idx);
            double weight = qValues.get(state, choice.getClass(), choice);
            r -= weight;
            if (r <= 0.0) {
                break;
            }
        }
        return idx;
    }

    public void penalizeSelected(S state, Object action) {
        // give a negative reward to the selected choice
        rewardAction(state, action, STEP_PENALTY_REWARD);
    }

    public void rewardNewTimeline(S state, Object action) {
        rewardAction(state, action, NEW_TIMELINE_REWARD);
    }

    public int getNumStates() {
        return qValues.size();
    }

    public int getNumActions() {
        int result = 0;
        for (S state : qValues.getStates()) {
            ChoiceQTable.ChoiceQStateEntry clsMap = qValues.get(state);
            for (Object cls : clsMap.getClasses()) {
                result += clsMap.get((Class) cls).size();
            }
        }
        return result;
    }


    public void printQTable() {
        PExLogger.logVerbose("--------------------");
        PExLogger.logVerbose("Q Table");
        PExLogger.logVerbose("--------------------");
        PExLogger.logVerbose(String.format("  #QStates = %d", qValues.size()));
        for (S state : qValues.getStates()) {
            ChoiceQTable.ChoiceQStateEntry stateEntry = qValues.get(state);
            String stateStr = String.valueOf(state);
            if (stateStr.length() > 10) {
                stateStr = stateStr.substring(0, 5).concat("...");
            }

            for (Object obj : stateEntry.getClasses()) {
                Class cls = (Class) obj;
                ChoiceQTable.ChoiceQClassEntry classEntry = stateEntry.get(cls);
                if (classEntry.size() <= 1) {
                    continue;
                }
                Object bestAction = classEntry.getBestAction();
                if (bestAction != null) {
                    double maxQ = classEntry.get(bestAction);
                    PExLogger.logVerbose(
                            String.format(
                                    "  %s [%s] -> %s -> %.2f\t%s",
                                    stateStr, cls.getSimpleName(), bestAction, maxQ, classEntry));
                }
            }
        }
    }
}
