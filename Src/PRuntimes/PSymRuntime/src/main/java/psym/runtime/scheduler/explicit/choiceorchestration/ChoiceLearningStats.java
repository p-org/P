package psym.runtime.scheduler.explicit.choiceorchestration;

import lombok.Getter;
import psym.runtime.machine.events.Message;
import psym.runtime.logger.PSymLogger;
import psym.runtime.machine.Machine;
import psym.runtime.scheduler.Scheduler;
import psym.valuesummary.Guard;
import psym.valuesummary.PrimitiveVS;
import psym.valuesummary.ValueSummary;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;

public class ChoiceLearningStats<S, A> implements Serializable {
    @Getter
    private static final double defaultQValue = 0;
    @Getter
    private static final double defaultReward = -1;
    @Getter
    private static final double ALPHA = 0.3;
    @Getter
    private static final double GAMMA = 0.7;
    @Getter
    private final ChoiceComparator choiceComparator = new ChoiceComparator();
    private final ChoiceQTable<S, A> qValues;
    /**
     * State hash corresponding to current environment state
     */
    @Getter
    private Object programStateHash = null;

    public ChoiceLearningStats() {
        qValues = new ChoiceQTable();
    }

    public static Class getActionClass(ValueSummary action) {
        if (action instanceof PrimitiveVS) {
            PrimitiveVS pv = (PrimitiveVS) action;
            return pv.getValueClass();
        }
        return action.getClass();
    }

    public void rewardIteration(ChoiceQTable.ChoiceQTableKey<S, A> stateActions, double reward, ChoiceLearningRewardMode rewardMode) {
        switch (rewardMode) {
            case None:
                // do nothing
                break;
            case Fixed:
                reward(stateActions, defaultReward);
                break;
            case Coverage:
                reward(stateActions, (reward - defaultReward));
                break;
            default:
                assert (false);
        }
    }

    public void rewardStep(ChoiceQTable.ChoiceQTableKey<S, A> stateActions, int reward) {
//        reward(stateActions, BigDecimal.valueOf(reward));
    }

    private void reward(ChoiceQTable.ChoiceQTableKey<S, A> stateActions, double reward) {
        if (reward == getDefaultQValue()) {
            return;
        }
        S state = stateActions.getState();
        ChoiceQTable.ChoiceQStateEntry stateEntry = qValues.get(state);

        for (Class cls : stateActions.getActions().getClasses()) {
            ChoiceQTable.ChoiceQClassEntry classEntry = stateEntry.get(cls);
            double maxQ = classEntry.getMaxQ();

            for (A action : stateActions.getActions().get(cls)) {
                double oldVal = classEntry.get(action);
                double newVal = (1 - ALPHA)*oldVal + ALPHA*(reward + GAMMA*maxQ);
                classEntry.update(action, newVal);
            }
        }
    }

    public int numQStates() {
        return qValues.size();
    }

    public int numQValues() {
        int result = 0;
        for (S state : qValues.getStates()) {
            ChoiceQTable.ChoiceQStateEntry stateEntry = qValues.get(state);
            for (Object cls : stateEntry.getClasses()) {
                result += stateEntry.get((Class) cls).size();
            }
        }
        return result;
    }

    public void printQTable() {
        PSymLogger.log("--------------------");
        PSymLogger.info("Q Table");
        PSymLogger.log("--------------------");
        PSymLogger.info(String.format("  #QStates = %d", qValues.size()));
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
//                PSymLogger.info(String.format("  %s [%s] -> %s", stateStr, cls.getSimpleName(), classEntry));
                Object bestAction = classEntry.getBestAction();
                if (bestAction != null) {
                    double maxQ = classEntry.get(bestAction);
                    PSymLogger.info(String.format("  %s [%s] -> %s -> %.10f\t%s", stateStr, cls.getSimpleName(), bestAction, maxQ, classEntry));
                }
            }
        }
    }

    public double getQvalue(S state, Class cls, A action) {
        return qValues.get(state, cls, action);
    }

    public double getCurrentQvalue(ValueSummary action) {
        Class cls = getActionClass(action);
        return getQvalue((S) programStateHash, cls, (A) getActionHash(cls, action));
    }

    public int getNumQStates() {
        return qValues.size();
    }

    public Object getActionHash(Class cls, ValueSummary action) {
        return action.toString();
    }

    public void setProgramStateHash(Scheduler sch, ChoiceLearningStateMode mode, PrimitiveVS<Machine> lastChoice) {
        switch (mode) {
            case None:
                setProgramHashNone();
                break;
            case SchedulerDepth:
                setProgramHashDepth(sch.getDepth());
                break;
            case LastStep:
                setProgramHashLastStep(lastChoice);
                break;
            case MachineState:
                setProgramHashMachineState(sch);
                break;
            case MachineStateAndLastStep:
                setProgramHashMachineStateAndLastStep(sch, lastChoice);
                break;
            case MachineStateAndEvents:
                setProgramHashMachineStateEvents(sch);
                break;
            case FullState:
                setProgramHashFullState(sch);
                break;
            default:
                assert (false);
        }
    }

    private void setProgramHashNone() {
        programStateHash = 0;
    }

    private void setProgramHashDepth(int depth) {
        programStateHash = depth;
    }

    private void setProgramHashLastStep(PrimitiveVS<Machine> lastChoice) {
        if (lastChoice != null) {
            List<Object> features = new ArrayList<>();
            for (Machine m : lastChoice.getValues()) {
                addMachineFeatures(features, m);
            }
            programStateHash = features.toString();
        }
    }

    private void setProgramHashMachineState(Scheduler sch) {
        List<Object> features = new ArrayList<>();
        for (Machine m : sch.getMachines()) {
            features.add(m);
            features.addAll(m.getCurrentState().getValues());
        }
        programStateHash = features.toString();
    }

    private void setProgramHashMachineStateAndLastStep(Scheduler sch, PrimitiveVS<Machine> lastChoice) {
        List<Object> features = new ArrayList<>();
        for (Machine m : sch.getMachines()) {
            features.add(m);
            features.addAll(m.getCurrentState().getValues());
        }
        if (lastChoice != null) {
            for (Machine m : lastChoice.getValues()) {
                addMachineFeatures(features, m);
            }
        }
        programStateHash = features.toString();
    }

    private void setProgramHashMachineStateEvents(Scheduler sch) {
        List<Object> features = new ArrayList<>();
        for (Machine m : sch.getMachines()) {
            addMachineFeatures(features, m);
        }
        programStateHash = features.toString();
    }

    private void addMachineFeatures(List<Object> features, Machine m) {
        features.add(m);
        features.addAll(m.getCurrentState().getValues());
        if (!m.sendBuffer.isEmpty()) {
            Message msg = m.sendBuffer.peek(Guard.constTrue());
            features.addAll(msg.getTarget().getValues());
            //                    features.add(msg.getPayloadFor(event));
            features.addAll(msg.getEvent().getValues());
        }
    }

    private void setProgramHashFullState(Scheduler sch) {
        List<Object> features = new ArrayList<>();
        for (Machine m : sch.getMachines()) {
            features.add(m);
            features.add(m.getLocalState());
        }
        programStateHash = features.toString();
    }

    private class ChoiceComparator implements Comparator<ValueSummary>, Serializable {
        public ChoiceComparator() {
        }

        @Override
        public int compare(ValueSummary lhs, ValueSummary rhs) {
            return Double.compare(getCurrentQvalue(rhs), getCurrentQvalue(lhs));
        }
    }

}
