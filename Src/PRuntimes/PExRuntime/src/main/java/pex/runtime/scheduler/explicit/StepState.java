package pex.runtime.scheduler.explicit;

import lombok.Getter;
import pex.runtime.machine.MachineLocalState;
import pex.runtime.machine.PMachine;

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
    private SortedSet<PMachine> machines = new TreeSet<>();

    /**
     * Local state of each machine (if present in machineSet)
     */
    private Map<PMachine, MachineLocalState> machineLocalStates = new HashMap<>();

    public StepState copyState() {
        StepState stepState = new StepState();

        stepState.machines = new TreeSet<>(this.machines);

        stepState.machineLocalStates = new HashMap<>();
        for (PMachine machine : this.machines) {
            stepState.machineLocalStates.put(machine, machine.copyMachineState());
        }

        assert (stepState.machines.size() == stepState.machineLocalStates.size());
        return stepState;
    }

    public void clear() {
        machines.clear();
        machineLocalStates.clear();
    }


    public void resetToZero(SortedSet<PMachine> allMachines) {
        for (PMachine machine : allMachines) {
            machine.reset();
        }
        machines.clear();
    }

    public void setTo(SortedSet<PMachine> allMachines, StepState input) {
        machines = new TreeSet<>(input.machines);
        machineLocalStates = new HashMap<>(input.machineLocalStates);
        assert (machines.size() == machineLocalStates.size());

        for (PMachine machine : allMachines) {
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
        machines.add(machine);
    }

    /**
     * Get the number of machines of a given type in the schedule.
     *
     * @param type Machine type
     * @return Number of machine of a given type
     */
    public int getMachineCount(Class<? extends PMachine> type) {
        int result = 0;
        for (PMachine m : machines) {
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
        for (PMachine m : machines) {
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
        for (PMachine machine : machines) {
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
