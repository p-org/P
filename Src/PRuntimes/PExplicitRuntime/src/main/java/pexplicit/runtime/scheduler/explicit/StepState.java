package pexplicit.runtime.scheduler.explicit;

import lombok.Getter;
import lombok.Setter;
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
     * Step number
     */
    @Getter
    @Setter
    private int stepNumber = 0;

    /**
     * Choice number
     */
    @Getter
    @Setter
    private int choiceNumber = 0;

    /**
     * Mapping from machine type to list of machine instances
     */
    @Getter
    private Map<Class<? extends PMachine>, List<PMachine>> machineListByType = new HashMap<>();

    /**
     * Set of machines
     */
    @Getter
    private SortedSet<PMachine> machineSet = new TreeSet<>();

    /**
     * Local state of each machine (null if not in machineSet)
     */
    @Getter
    private List<MachineLocalState> machineLocalStates = new ArrayList<>();

    public StepState copy() {
        StepState stepState = new StepState();

        stepState.stepNumber = this.stepNumber;
        stepState.choiceNumber = this.choiceNumber;
        stepState.machineListByType = new HashMap<>(this.machineListByType);
        stepState.machineSet = new TreeSet<>(this.machineSet);
        stepState.machineLocalStates = new ArrayList<>(this.machineLocalStates);
        return stepState;
    }


    public void resetToZero() {
        this.stepNumber = 0;
        this.choiceNumber = 0;
        for (PMachine machine: PExplicitGlobal.getMachineSet()) {
            machine.reset();
        }
        machineListByType.clear();
        machineSet.clear();
        machineLocalStates.clear();
    }

    public void setTo(StepState input) {
        stepNumber = input.stepNumber;
        choiceNumber = input.choiceNumber;
        machineListByType = input.machineListByType;
        machineSet = input.machineSet;
        machineLocalStates = input.machineLocalStates;

        int i = 0;
        for (PMachine machine: PExplicitGlobal.getMachineSet()) {
            MachineLocalState ms = machineLocalStates.get(i++);
            if (ms == null) {
                machine.reset();
            } else {
                machine.setMachineState(ms);
            }
        }
    }

    public void storeMachinesState() {
        machineLocalStates.clear();
        for (PMachine machine : PExplicitGlobal.getMachineSet()) {
            MachineLocalState ms = null;
            if (machineSet.contains(machine)) {
                ms = machine.copyMachineState();
            }
            machineLocalStates.add(ms);
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
        s.append(String.format("@%d::%d\n", stepNumber, choiceNumber));

        int i = 0;
        for (PMachine machine : machineSet) {
            s.append(String.format("%s:\n", machine));
            List<String> fields = machine.getLocalVarNames();
            List<Object> values = machineLocalStates.get(i++).getLocals();
            int j = 0;
            for (String field: fields) {
                Object val = values.get(j++);
                s.append(String.format("  %s -> %s\n", field, val));
            }
        }
        return s.toString();
    }
}
