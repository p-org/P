package psym.runtime.scheduler.choiceorchestration;

import lombok.Getter;
import psym.utils.GlobalData;
import psym.valuesummary.ValueSummary;

import java.io.Serializable;
import java.math.BigDecimal;
import java.util.*;

public class ChoiceQTable<S, A> implements Serializable {
    private final Map<S, ChoiceQStateEntry<A>> table = new HashMap<>();

    public BigDecimal get(S state, Class cls, A action) {
        if (!table.containsKey(state)) {
            table.put(state, new ChoiceQStateEntry());
        }
        return table.get(state).get(cls, action);
    }

    public int size() {
        return table.size();
    }

    public ChoiceQStateEntry get(S state) {
        if (!table.containsKey(state)) {
            table.put(state, new ChoiceQStateEntry());
        }
        return table.get(state);
    }

    public Set<S> getStates() {
        return table.keySet();
    }

    @Override
    public String toString() {
        StringBuilder out = new StringBuilder();
        out.append("{ ");
        for (Map.Entry<S, ChoiceQStateEntry<A>> entry : table.entrySet()) {
            out.append(entry.getKey().toString());
            out.append(" -> ");
            out.append(entry.getValue().toString());
            out.append(", ");
        }
        out.append(" }");
        return out.toString();
    }

    public static class ChoiceQTableKey<S, A> implements Serializable {
        @Getter
        S state;
        @Getter
        ChoiceQTable.ChoiceQStateKey<A> actions;

        public ChoiceQTableKey() {
            this(null, new ChoiceQTable.ChoiceQStateKey());
        }

        public ChoiceQTableKey(S s, ChoiceQTable.ChoiceQStateKey<A> a) {
            this.state = s;
            this.actions = a;
        }

        public void clear() {
            this.state = null;
            actions.clear();
        }

        @Override
        public String toString() {
            String out = "{ " +
                    state.toString() +
                    " -> " +
                    actions.toString() +
                    " }";
            return out;
        }
    }

    public static class ChoiceQStateKey<A> implements Serializable {
        Map<Class, List<A>> table = new HashMap<>();

        public void add(ValueSummary action) {
            Class cls = ChoiceLearningStats.getActionClass(action);
            if (!table.containsKey(cls)) {
                table.put(cls, new ArrayList<>());
            }
            table.get(cls).add((A) GlobalData.getChoiceLearningStats().getActionHash(cls, action));
        }

        public List<A> get(Class cls) {
            return table.getOrDefault(cls, new ArrayList<>());
        }

        public Set<Class> getClasses() {
            return table.keySet();
        }

        public void clear() {
            table.clear();
        }

        @Override
        public String toString() {
            StringBuilder out = new StringBuilder();
            out.append("{ ");
            for (Map.Entry<Class, List<A>> entry : table.entrySet()) {
                out.append(entry.getKey().toString());
                out.append(" -> ");
                out.append(entry.getValue().toString());
                out.append(", ");
            }
            out.append(" }");
            return out.toString();
        }
    }

    public class ChoiceQStateEntry<A> implements Serializable {
        private final Map<Class, ChoiceQTable.ChoiceQClassEntry> table = new HashMap<>();

        public BigDecimal get(Class cls, A action) {
            if (!table.containsKey(cls)) {
                table.put(cls, new ChoiceQClassEntry());
            }
            return table.get(cls).get(action);
        }

        public ChoiceQClassEntry<A> get(Class cls) {
            if (!table.containsKey(cls)) {
                table.put(cls, new ChoiceQClassEntry<A>());
            }
            return table.get(cls);
        }

        public Set<Class> getClasses() {
            return table.keySet();
        }

        @Override
        public String toString() {
            StringBuilder out = new StringBuilder();
            out.append("{ ");
            for (Map.Entry<Class, ChoiceQTable.ChoiceQClassEntry> entry : table.entrySet()) {
                out.append(entry.getKey().toString());
                out.append(" -> ");
                out.append(entry.getValue().toString());
                out.append(", ");
            }
            out.append(" }");
            return out.toString();
        }
    }

    public class ChoiceQClassEntry<A> implements Serializable {
        private final Map<A, BigDecimal> table = new HashMap<>();

        public BigDecimal get(A action) {
            if (!table.containsKey(action)) {
                table.put(action, ChoiceLearningStats.getDefaultQValue());
            }
            return table.get(action);
        }

        public void update(A action, BigDecimal val) {
            assert (table.containsKey(action));
            table.put(action, val);
        }

        public BigDecimal getMaxQ() {
            if (table.isEmpty()) {
                return ChoiceLearningStats.getDefaultQValue();
            } else {
                return Collections.max(table.values());
            }
        }

        public A getBestAction() {
            if (!table.isEmpty()) {
                BigDecimal maxQ = getMaxQ();
                for (A action : table.keySet()) {
                    if (get(action) == maxQ) {
                        return action;
                    }
                }
            }
            return null;
        }

        public int size() {
            return table.size();
        }

        @Override
        public String toString() {
            StringBuilder out = new StringBuilder();
            out.append("{ ");
            for (Map.Entry<A, BigDecimal> entry : table.entrySet()) {
                out.append(entry.getKey().toString());
                out.append(" -> ");
                out.append(String.format("%.5f", entry.getValue()));
                out.append(", ");
            }
            out.append(" }");
            return out.toString();
        }
    }
}
