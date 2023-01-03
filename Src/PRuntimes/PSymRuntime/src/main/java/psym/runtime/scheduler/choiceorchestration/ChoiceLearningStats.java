package psym.runtime.scheduler.choiceorchestration;

import lombok.Getter;
import psym.runtime.Event;
import psym.runtime.Message;
import psym.runtime.logger.PSymLogger;
import psym.runtime.machine.Machine;
import psym.runtime.machine.State;
import psym.runtime.scheduler.Scheduler;
import psym.valuesummary.Guard;
import psym.valuesummary.PrimitiveVS;
import psym.valuesummary.ValueSummary;

import java.io.Serializable;
import java.math.BigDecimal;
import java.util.*;

public class ChoiceLearningStats<S, A> implements Serializable {
    @Getter
    private static BigDecimal defaultQValue = BigDecimal.ZERO;
    @Getter
    private static BigDecimal ALPHA = BigDecimal.valueOf(0.5);
    @Getter
    private static BigDecimal GAMMA = BigDecimal.valueOf(0.5);


    /** State hash corresponding to current environment state */
    @Getter
    private Object programStateHash = null;

    @Getter
    private ChoiceComparator choiceComparator = new ChoiceComparator();
    private ChoiceQTable<S, A> qValues;

    private class ChoiceComparator implements Comparator<ValueSummary>, Serializable {
        public ChoiceComparator() {}

        @Override
        public int compare(ValueSummary lhs, ValueSummary rhs) {
            return getCurrentQvalue(rhs).compareTo(getCurrentQvalue(lhs));
        }
    }


    public ChoiceLearningStats() {
        qValues = new ChoiceQTable();
    }

    public void rewardIteration(ChoiceQTable.ChoiceQTableKey<S, A> stateActions, BigDecimal reward) {
        reward(stateActions, reward);
    }

    public void rewardStep(ChoiceQTable.ChoiceQTableKey<S, A> stateActions, int reward) {
//        reward(stateActions, BigDecimal.valueOf(reward));
    }

    private void reward(ChoiceQTable.ChoiceQTableKey<S, A> stateActions, BigDecimal reward) {
        if (reward.equals(getDefaultQValue())) {
            return;
        }
        S state = stateActions.getState();
        ChoiceQTable.ChoiceQStateEntry stateEntry = qValues.get(state);

        for (Class cls: stateActions.getActions().getClasses()) {
            ChoiceQTable.ChoiceQClassEntry classEntry = stateEntry.get(cls);
            BigDecimal maxQ = classEntry.getMaxQ();

            for (A action: stateActions.getActions().get(cls)) {
                BigDecimal oldVal = classEntry.get(action);
                BigDecimal newVal = BigDecimal.valueOf(1).subtract(ALPHA).multiply(oldVal)
                                    .add(ALPHA.multiply(reward.add(GAMMA.multiply(maxQ))));
                classEntry.update(action, newVal);
            }
        }
    }

    public void printQTable() {
        PSymLogger.log("--------------------");
        PSymLogger.info("Q Table");
        PSymLogger.log("--------------------");
        PSymLogger.info(String.format("  #QStates = %d", qValues.size()));
        for (S state: qValues.getStates()) {
            ChoiceQTable.ChoiceQStateEntry stateEntry = qValues.get(state);
            String stateStr = String.valueOf(state);
            if (stateStr.length() > 10) {
                stateStr = stateStr.substring(0, 5).concat("...");
            }

            for (Object obj: stateEntry.getClasses()) {
                Class cls = (Class) obj;
                ChoiceQTable.ChoiceQClassEntry classEntry = stateEntry.get(cls);
                if (classEntry.size() <= 1) {
                    continue;
                }
//                PSymLogger.info(String.format("  %s [%s] -> %s", stateStr, cls.getSimpleName(), classEntry));
                Object bestAction = classEntry.getBestAction();
                if (bestAction != null) {
                    BigDecimal maxQ = classEntry.get(bestAction);
                    PSymLogger.info(String.format("  %s [%s] -> %s -> %.10f\t%s", stateStr, cls.getSimpleName(), bestAction, maxQ, classEntry));
                }
            }
        }
    }

    public BigDecimal getQvalue(S state, Class cls, A action) {
        return qValues.get(state, cls, action);
    }

    public BigDecimal getCurrentQvalue(ValueSummary action) {
        Class cls = getActionClass(action);
        return getQvalue((S) programStateHash, cls, (A) getActionHash(cls, action));
    }

    public int getNumQStates() {
        return qValues.size();
    }

    public static Class getActionClass(ValueSummary action) {
        if (action instanceof PrimitiveVS) {
            PrimitiveVS pv = (PrimitiveVS) action;
            return pv.getValueClass();
        }
        return action.getClass();
    }

    public Object getActionHash(Class cls, ValueSummary action) {
        return action.toString();
    }

    public void setProgramStateHash(Scheduler sch) {
//        List<Object> features = new ArrayList<>();
//        features.add(sch.getChoiceDepth());
//        for (Machine m: sch.getMachines()) {
//            features.add(addMachineFeatures(m));
//        }
//        programStateHash = features.toString();
        programStateHash = sch.getDepth();
    }

    private List<Object> addMachineFeatures(Machine source) {
        List<Object> features = new ArrayList<>();
        features.add(source);
//        features.add(source.getLocalState());
        for (State state: source.getCurrentState().getValues()) {
            features.add(state);
        }
        if (!source.sendBuffer.isEmpty()) {
            Message msg = source.sendBuffer.peek(Guard.constTrue());
            for (Machine target: msg.getTarget().getValues()) {
                features.add(target);
            }
            for (Event event: msg.getEvent().getValues()) {
                features.add(event);
                features.add(msg.getPayloadFor(event));
            }
        }
        return features;
    }
}
