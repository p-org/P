package pexplicit.runtime.scheduler.explicit.choiceselector;

import lombok.Getter;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.scheduler.explicit.ExplicitSearchScheduler;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.List;

public class ChoiceQL implements Serializable {
    @Getter
    private static final int defaultQValue = 0;
    private static final double ALPHA = 0.5;
    private static final double GAMMA = 0.05;
    private final ChoiceQTable<Integer, Object> qValues;
    private final List<Object> currActions = new ArrayList<>();
    /**
     * Details about the current step
     */
    private Integer currState = 0;
    private int currNumTimelines = 0;

    public ChoiceQL() {
        qValues = new ChoiceQTable();
    }

    private void rewardAction(Object action, int reward) {
        ChoiceQTable.ChoiceQStateEntry stateEntry = qValues.get(currState);
        ChoiceQTable.ChoiceQClassEntry classEntry = stateEntry.get(action.getClass());
        int maxQ = classEntry.getMaxQ();
        int oldVal = classEntry.get(action);
        int newVal = (int) ((1 - ALPHA) * oldVal + ALPHA * (reward + GAMMA * maxQ));
        classEntry.update(action, newVal);
    }

    private void setStateTimelineAbstraction(ExplicitSearchScheduler sch) {
        List<Integer> features = new ArrayList<>();
        for (PMachine m : sch.getStepState().getMachineSet()) {
            features.add(m.hashCode());
            features.add(m.getHappensBeforePairs().hashCode());
        }
        currState = features.hashCode();
    }

    public void startStep(ExplicitSearchScheduler sch) {
//        printQTable();

        // set reward amount
        int reward = -100;
        if (sch.getTimelines().size() > currNumTimelines) {
            reward = 100;
        }
        // reward last actions
        for (Object action : currActions) {
            rewardAction(action, reward);
        }

        // set number of timelines at start of step
        currNumTimelines = sch.getTimelines().size();

        // set state at start of step
        setStateTimelineAbstraction(sch);

        // reset current actions at start of step
        currActions.clear();
    }

    public int selectChoice(List<?> choices) {
        int maxVal = Integer.MIN_VALUE;
        int selected = 0;
        for (int i = 0; i < choices.size(); i++) {
            Object choice = choices.get(i);
            int val = qValues.get(currState, choice.getClass(), choice);
            if (val > maxVal) {
                maxVal = val;
                selected = i;
            }
        }
        return selected;
    }

    public void addChoice(Object choice) {
        currActions.add(choice);
    }

    public int getNumStates() {
        return qValues.size();
    }

    public int getNumActions() {
        int result = 0;
        for (Integer state : qValues.getStates()) {
            ChoiceQTable.ChoiceQStateEntry clsMap = qValues.get(state);
            for (Object cls : clsMap.getClasses()) {
                result += clsMap.get((Class) cls).size();
            }
        }
        return result;
    }


    public void printQTable() {
        PExplicitLogger.logVerbose("--------------------");
        PExplicitLogger.logVerbose("Q Table");
        PExplicitLogger.logVerbose("--------------------");
        PExplicitLogger.logVerbose(String.format("  #QStates = %d", qValues.size()));
        for (Integer state : qValues.getStates()) {
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
                    int maxQ = classEntry.get(bestAction);
                    PExplicitLogger.logVerbose(
                            String.format(
                                    "  %s [%s] -> %s -> %d\t%s",
                                    stateStr, cls.getSimpleName(), bestAction, maxQ, classEntry));
                }
            }
        }
    }
}
