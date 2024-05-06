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
     * Mapping from machine type to list of machine instances
     */
    private Map<Class<? extends PMachine>, List<PMachine>> machineListByType = new HashMap<>();

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

        stepState.machineListByType = new HashMap<>(this.machineListByType);
        stepState.machineSet = new TreeSet<>(this.machineSet);

        stepState.machineLocalStates = new HashMap<>();
        for (PMachine machine : this.machineSet) {
            stepState.machineLocalStates.put(machine, machine.copyMachineState());
        }

        assert (stepState.machineSet.size() == stepState.machineLocalStates.size());
        return stepState;
    }

    public void clear() {
        machineListByType.clear();
        machineSet.clear();
        machineLocalStates.clear();
    }


    public void resetToZero() {
        for (PMachine machine : PExplicitGlobal.getMachineSet()) {
            machine.reset();
        }
        machineListByType.clear();
        machineSet.clear();
    }

    public void setTo(StepState input) {
        machineListByType = new HashMap<>(input.machineListByType);
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
        if (!machineListByType.containsKey(machine.getClass())) {
            machineListByType.put(machine.getClass(), new ArrayList<>());
        }
        machineListByType.get(machine.getClass()).add(machine);
        machineSet.add(machine);
    }

    /**
     * Check if a machine of a given type and index exists in the schedule.
     *
     * @param type Machine type
     * @param idx  Machine index
     * @return true if machine is in this schedule, false otherwise
     */
    public boolean hasMachine(Class<? extends PMachine> type, int idx) {
        if (!machineListByType.containsKey(type))
            return false;
        return idx < machineListByType.get(type).size();
    }

    /**
     * Get a machine of a given type and index.
     *
     * @param type Machine type
     * @param idx  Machine index
     * @return Machine
     */
    public PMachine getMachine(Class<? extends PMachine> type, int idx) {
        assert (hasMachine(type, idx));
        return machineListByType.get(type).get(idx);
    }

    /**
     * Get the number of machines of a given type in the schedule.
     *
     * @param type Machine type
     * @return Number of machine of a given type
     */
    public int getMachineCount(Class<? extends PMachine> type) {
        return machineListByType.getOrDefault(type, new ArrayList<>()).size();
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
