package pexplicit.runtime.scheduler.explicit;

import lombok.Getter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.machine.MachineLocalState;
import pexplicit.runtime.machine.PMachine;

import java.io.Serializable;
import java.util.*;

/**
 * Represents the schedule state at a particular step
 */
public class StepState implements Serializable {
    /**
     * Set of machines
     */
    @Getter
    private SortedSet<PMachine> machineSet = new TreeSet<>();

    /**
     * Local state of each machine (if present in machineSet)
     */
    private Map<PMachine, MachineLocalState> machineLocalStates = new HashMap<>();

    public StepState copyState() {
        StepState stepState = new StepState();

        stepState.machineSet = new TreeSet<>(this.machineSet);

        stepState.machineLocalStates = new HashMap<>();
        for (PMachine machine : this.machineSet) {
            stepState.machineLocalStates.put(machine, machine.copyMachineState());
        }

        assert (stepState.machineSet.size() == stepState.machineLocalStates.size());
        return stepState;
    }

    public void clear() {
        machineSet.clear();
        machineLocalStates.clear();
    }


    public void resetToZero() {
        for (PMachine machine : PExplicitGlobal.getMachineSet()) {
            machine.reset();
        }
        machineSet.clear();
    }

    public void setTo(StepState input) {
        machineSet = new TreeSet<>(input.machineSet);
        machineLocalStates = new HashMap<>(input.machineLocalStates);
        assert (machineSet.size() == machineLocalStates.size());

        for (PMachine machine : PExplicitGlobal.getMachineSet()) {
            MachineLocalState ms = machineLocalStates.get(machine);
            if (ms == null) {
                machine.reset();
            } else {
                machine.setMachineState(ms);
            }
        }
    }

    /**
     * Add a machine to the schedule.
     *
     * @param machine Machine to add
     */
    public void makeMachine(PMachine machine) {
        machineSet.add(machine);
    }

    /**
     * Get the number of machines of a given type in the schedule.
     *
     * @param type Machine type
     * @return Number of machine of a given type
     */
    public int getMachineCount(Class<? extends PMachine> type) {
        int result = 0;
        for (PMachine m : machineSet) {
            if (type.isInstance(m)) {
                result++;
            }
        }
        return result;
    }

    public Object getTimeline() {
//        return getTimelineString();
        return getTimelineString().hashCode();
    }

    public String getTimelineString() {
        StringBuilder s = new StringBuilder();
        for (PMachine m : machineSet) {
            MachineLocalState ms = machineLocalStates.get(m);
            if (ms != null) {
                s.append(String.format("%s -> %s, ", m, ms.getHappensBeforePairs()));
            }
        }
        return s.toString();
    }

    @Override
    public String toString() {
        if (this == null) {
            return "null";
        }

        StringBuilder s = new StringBuilder();
        for (PMachine machine : machineSet) {
            s.append(String.format("%s:\n", machine));
            List<String> fields = machine.getLocalVarNames();
            List<Object> values = machineLocalStates.get(machine).getLocals();
            int j = 0;
            for (String field : fields) {
                Object val = values.get(j++);
                s.append(String.format("  %s -> %s\n", field, val));
            }
        }
        return s.toString();
    }
}
