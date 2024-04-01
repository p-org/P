package pexplicit.runtime.scheduler;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.machine.PMachine;
import pexplicit.values.PValue;

import java.io.Serializable;
import java.util.*;

/**
 * Represents a single (possibly partial) schedule.
 */
public class Schedule implements Serializable {
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
     * List of choices
     */
    @Getter
    @Setter
    private List<Choice> choices = new ArrayList<>();

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
     * Constructor
     */
    public Schedule() {
    }

    /**
     * Get a fresh new choice
     *
     * @return New choice object
     */
    public Choice newChoice() {
        return new Choice();
    }

    /**
     * Get the choice at a choice depth
     *
     * @param idx Choice depth
     * @return Choice at depth idx
     */
    public Choice getChoice(int idx) {
        return choices.get(idx);
    }

    /**
     * Set the choice at a choice depth.
     *
     * @param idx    Choice depth
     * @param choice Choice object
     */
    public void setChoice(int idx, Choice choice) {
        choices.set(idx, choice);
    }

    /**
     * Clear choice at a choice depth
     *
     * @param idx Choice depth
     */
    public void clearChoice(int idx) {
        choices.get(idx).clear();
    }

    /**
     * Get the number of unexplored choices in this schedule
     *
     * @return Number of unexplored choices
     */
    public int getNumUnexploredChoicesInSchedule() {
        int numUnexplored = 0;
        for (Choice c : choices) {
            if (c.isUnexploredNonEmpty()) {
                numUnexplored++;
            }
        }
        return numUnexplored;
    }

    /**
     * Get the number of unexplored data choices in this schedule
     *
     * @return Number of unexplored data choices
     */
    public int getNumUnexploredDataChoicesInSchedule() {
        int numUnexplpredData = 0;
        for (Choice c : choices) {
            if (c.isUnexploredDataChoicesNonEmpty()) {
                numUnexplpredData++;
            }
        }
        return numUnexplpredData;
    }

    /**
     * Set the current schedule choice at a choice depth.
     *
     * @param choice Machine to set as current schedule choice
     * @param idx    Choice depth
     */
    public void setCurrentScheduleChoice(PMachine choice, int idx) {
        if (idx >= choices.size()) {
            choices.add(newChoice());
        }
        choices.get(idx).setCurrentScheduleChoice(choice);
    }

    /**
     * Set the current data choice at a choice depth.
     *
     * @param choice PValue to set as current data choice
     * @param idx    Choice depth
     */
    public void setCurrentDataChoice(PValue<?> choice, int idx) {
        if (idx >= choices.size()) {
            choices.add(newChoice());
        }
        choices.get(idx).setCurrentDataChoice(choice);
    }

    /**
     * Set unexplored schedule choices at a choice depth.
     *
     * @param machines List of machines to set as unexplored schedule choices
     * @param idx      Choice depth
     */
    public void setUnexploredScheduleChoice(List<PMachine> machines, int idx) {
        if (idx >= choices.size()) {
            choices.add(newChoice());
        }
        choices.get(idx).setUnexploredScheduleChoice(machines);
    }

    /**
     * Set unexplored data choices at a choice depth.
     *
     * @param values List of PValue to set as unexplored data choices
     * @param idx    Choice depth
     */
    public void setUnexploredDataChoice(List<PValue<?>> values, int idx) {
        if (idx >= choices.size()) {
            choices.add(newChoice());
        }
        choices.get(idx).setUnexploredDataChoice(values);
    }

    /**
     * Get the current schedule choice at a choice depth.
     *
     * @param idx Choice depth
     * @return Current schedule choice
     */
    public PMachine getCurrentScheduleChoice(int idx) {
        return choices.get(idx).getCurrentScheduleChoice();
    }

    /**
     * Get the current data choice at a choice depth.
     *
     * @param idx Choice depth
     * @return Current data choice
     */
    public PValue<?> getCurrentDataChoice(int idx) {
        return choices.get(idx).getCurrentDataChoice();
    }

    /**
     * Get unexplored schedule choices at a choice depth.
     *
     * @param idx Choice depth
     * @return List of machines, or null if index is invalid
     */
    public List<PMachine> getUnexploredScheduleChoice(int idx) {
        if (idx < size()) {
            return choices.get(idx).getUnexploredScheduleChoice();
        } else {
            return null;
        }
    }

    /**
     * Get unexplored data choices at a choice depth.
     *
     * @param idx Choice depth
     * @return List of PValue, or null if index is invalid
     */
    public List<PValue<?>> getUnexploredDataChoice(int idx) {
        if (idx < size()) {
            return choices.get(idx).getUnexploredDataChoice();
        } else {
            return null;
        }
    }

    /**
     * Clear current choices at a choice depth
     *
     * @param idx Choice depth
     */
    public void clearCurrent(int idx) {
        choices.get(idx).clearCurrent();
    }

    /**
     * Clear unexplored choices at a choice depth
     *
     * @param idx Choice depth
     */
    public void clearUnexplored(int idx) {
        choices.get(idx).clearUnexplored();
    }

    /**
     * Get the number of choices in the schedule
     *
     * @return Number of choices in the schedule
     */
    public int size() {
        return choices.size();
    }

    /**
     * Add a machine to the schedule.
     *
     * @param machine Machine to add
     */
    public void makeMachine(PMachine machine) {
        machineListByType.getOrDefault(machine.getClass(), new ArrayList<>()).add(machine);
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
}
