package pexplicit.runtime.scheduler;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.scheduler.explicit.StepState;
import pexplicit.values.PValue;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.List;

/**
 * Represents a single (possibly partial) schedule.
 */
public class Schedule implements Serializable {
    /**
     * List of choices
     */
    @Getter
    @Setter
    private List<Choice> choices = new ArrayList<>();

    /**
     * Step state at the start of a scheduler step.
     * Used in stateful backtracking
     */
    @Setter
    private transient StepState stepBeginState = null;

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
     * Clear choice at a choice depth
     *
     * @param idx Choice depth
     */
    public void clearChoice(int idx) {
        choices.get(idx).clearCurrent();
        choices.get(idx).clearUnexplored();
    }

    /**
     * Get the number of unexplored choices in this schedule
     *
     * @return Number of unexplored choices
     */
    public int getNumUnexploredChoices() {
        int numUnexplored = 0;
        for (Choice c : choices) {
            numUnexplored += c.unexploredScheduleChoices.size() + c.unexploredDataChoices.size();
        }
        return numUnexplored;
    }

    /**
     * Get the number of unexplored data choices in this schedule
     *
     * @return Number of unexplored data choices
     */
    public int getNumUnexploredDataChoices() {
        int numUnexplored = 0;
        for (Choice c : choices) {
            numUnexplored += c.unexploredDataChoices.size();
        }
        return numUnexplored;
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
    public void setUnexploredScheduleChoices(List<PMachine> machines, int idx) {
        if (idx >= choices.size()) {
            choices.add(newChoice());
        }
        choices.get(idx).setUnexploredScheduleChoices(machines);
        if (PExplicitGlobal.getConfig().isStatefulBacktrackEnabled()
                && !machines.isEmpty()
                && stepBeginState != null
                && stepBeginState.getStepNumber() != 0) {
            choices.get(idx).setChoiceStep(stepBeginState.copy());
        }
    }

    /**
     * Set unexplored data choices at a choice depth.
     *
     * @param values List of PValue to set as unexplored data choices
     * @param idx    Choice depth
     */
    public void setUnexploredDataChoices(List<PValue<?>> values, int idx) {
        if (idx >= choices.size()) {
            choices.add(newChoice());
        }
        choices.get(idx).setUnexploredDataChoices(values);
        if (PExplicitGlobal.getConfig().isStatefulBacktrackEnabled()
                && !values.isEmpty()
                && stepBeginState != null
                && stepBeginState.getStepNumber() != 0) {
            choices.get(idx).setChoiceStep(stepBeginState.copy());
        }
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
    public List<PMachine> getUnexploredScheduleChoices(int idx) {
        if (idx < size()) {
            return choices.get(idx).getUnexploredScheduleChoices();
        } else {
            return new ArrayList<>();
        }
    }

    /**
     * Get unexplored data choices at a choice depth.
     *
     * @param idx Choice depth
     * @return List of PValue, or null if index is invalid
     */
    public List<PValue<?>> getUnexploredDataChoices(int idx) {
        if (idx < size()) {
            return choices.get(idx).getUnexploredDataChoices();
        } else {
            return new ArrayList<>();
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
     * Get the number of choices in the schedule
     *
     * @return Number of choices in the schedule
     */
    public int size() {
        return choices.size();
    }
}
