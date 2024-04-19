package pexplicit.runtime.scheduler.explicit;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.machine.MachineState;
import pexplicit.runtime.machine.PMachine;

import java.io.Serializable;
import java.util.*;

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
     * Local state of each machine in machineSet in order
     */
    @Getter
    private List<MachineState> machineStates = new ArrayList<>();

    public StepState copy() {
        StepState stepState = new StepState();

        stepState.stepNumber = this.stepNumber;
        stepState.choiceNumber = this.choiceNumber;
        stepState.machineListByType = new HashMap<>(this.machineListByType);
        stepState.machineSet = new TreeSet<>(this.machineSet);
        stepState.machineStates = new ArrayList<>(this.machineStates);
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
        machineStates.clear();
    }

    public void setTo(StepState input) {
        stepNumber = input.stepNumber;
        choiceNumber = input.choiceNumber;
        machineListByType = input.machineListByType;
        machineSet = input.machineSet;
        machineStates = input.machineStates;

        int i = 0;
        for (PMachine machine : machineSet) {
            machine.setMachineState(machineStates.get(i++));
        }
        for (PMachine machine: PExplicitGlobal.getMachineSet()) {
            if (!machineSet.contains(machine)) {
                machine.reset();
            }
        }
    }

    public void storeMachinesState() {
        machineStates.clear();
        for (PMachine machine : machineSet) {
            MachineState ms = machine.copyMachineState();
            machineStates.add(ms);
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
        StringBuilder s = new StringBuilder();
        int i = 0;
        for (PMachine machine : machineSet) {
            s.append(String.format("%s::\n", machine));
            List<String> fields = machine.getLocalVarNames();
            List<Object> values = machineStates.get(i++).getLocals();
            int j = 0;
            for (String field: fields) {
                Object val = values.get(j++);
                s.append(String.format("  %s -> %s\n", field, val));
            }
        }
        return s.toString();
    }
}
