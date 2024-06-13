package pexplicit.runtime.scheduler;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.machine.PMachineId;
import pexplicit.runtime.scheduler.choice.Choice;
import pexplicit.runtime.scheduler.choice.DataChoice;
import pexplicit.runtime.scheduler.choice.ScheduleChoice;
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
     * Get the choice at a choice depth
     *
     * @param idx Choice depth
     * @return Choice at depth idx
     */
    public Choice getChoice(int idx) {
        return choices.get(idx);
    }

    /**
     * Remove choices after a choice depth
     *
     * @param choiceNum Choice depth
     */
    public void removeChoicesAfter(int choiceNum) {
        choices.subList(choiceNum + 1, choices.size()).clear();
    }

    /**
     * Set the schedule choice at a choice depth.
     *
     * @param stepNum    Step number
     * @param choiceNum  Choice number
     * @param current    Machine to set as current schedule choice
     */
    public void setScheduleChoice(int stepNum, int choiceNum, PMachineId current) {
        if (choiceNum == choices.size()) {
            choices.add(null);
        }
        assert (choiceNum < choices.size());
        if (PExplicitGlobal.getConfig().isStatefulBacktrackEnabled()
                && stepNum != 0) {
            assert (stepBeginState != null);
            choices.set(choiceNum, new ScheduleChoice(stepNum, choiceNum, current, stepBeginState));
        } else {
            choices.set(choiceNum, new ScheduleChoice(stepNum, choiceNum, current, null));
        }
    }

    /**
     * Set the data choice at a choice depth.
     *
     * @param stepNum    Step number
     * @param choiceNum  Choice number
     * @param current    PValue to set as current schedule choice
     */
    public void setDataChoice(int stepNum, int choiceNum, PValue<?> current) {
        if (choiceNum == choices.size()) {
            choices.add(null);
        }
        assert (choiceNum < choices.size());
        choices.set(choiceNum, new DataChoice(current));
    }

    /**
     * Get the current schedule choice at a choice depth.
     *
     * @param idx Choice depth
     * @return Current schedule choice
     */
    public PMachineId getCurrentScheduleChoice(int idx) {
        assert (choices.get(idx) instanceof ScheduleChoice);
        return ((ScheduleChoice) choices.get(idx)).getCurrent();
    }

    /**
     * Get the current data choice at a choice depth.
     *
     * @param idx Choice depth
     * @return Current data choice
     */
    public PValue<?> getCurrentDataChoice(int idx) {
        assert (choices.get(idx) instanceof DataChoice);
        return ((DataChoice) choices.get(idx)).getCurrent();
    }

    public ScheduleChoice getScheduleChoiceAt(int choiceNum) {
        for (int i = choiceNum; i >= 0; i--) {
            Choice c = choices.get(i);
            if (c instanceof ScheduleChoice scheduleChoice) {
                return scheduleChoice;
            }
        }
        return null;
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
