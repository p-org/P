package pexplicit.runtime.scheduler;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.scheduler.explicit.StepState;
import pexplicit.values.PValue;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.List;

/**
 * Represents a schedule or data choice
 */
@Getter
public class Choice implements Serializable {
    @Setter
    PMachine currentScheduleChoice = null;
    @Setter
    PValue<?> currentDataChoice = null;
    @Setter
    List<PMachine> unexploredScheduleChoices = new ArrayList<>();
    @Setter
    List<PValue<?>> unexploredDataChoices = new ArrayList<>();
    @Setter
    StepState choiceStep = null;

    /**
     * Constructor
     */
    public Choice() {
    }

    /**
     * Copy-constructor for Choice
     *
     * @param old The choice to copy
     */
    private Choice(Choice old) {
        currentScheduleChoice = old.currentScheduleChoice;
        currentDataChoice = old.currentDataChoice;
        unexploredScheduleChoices = new ArrayList<>(old.unexploredScheduleChoices);
        unexploredDataChoices = new ArrayList<>(old.unexploredDataChoices);
        choiceStep = old.choiceStep;
    }

    /**
     * Copy the Choice
     *
     * @return A new cloned copy of the Choice
     */
    public Choice getCopy() {
        return new Choice(this);
    }

    /**
     * Check if this choice has an unexplored choice remaining.
     *
     * @return true if this choice has an unexplored choice, false otherwise
     */
    public boolean isUnexploredNonEmpty() {
        return isUnexploredScheduleChoicesNonEmpty() || isUnexploredDataChoicesNonEmpty();
    }

    /**
     * Check if this choice has an unexplored schedule choice remaining.
     *
     * @return true if this choice has an unexplored schedule choice, false otherwise
     */
    public boolean isUnexploredScheduleChoicesNonEmpty() {
        return !getUnexploredScheduleChoices().isEmpty();
    }

    /**
     * Check if this choice has an unexplored data choice remaining.
     *
     * @return true if this choice has an unexplored data choice, false otherwise
     */
    public boolean isUnexploredDataChoicesNonEmpty() {
        return !getUnexploredDataChoices().isEmpty();
    }

    /**
     * Clear current choices
     */
    public void clearCurrent() {
        currentScheduleChoice = null;
        currentDataChoice = null;
    }

    /**
     * Clean unexplored choices
     */
    public void clearUnexplored() {
        unexploredScheduleChoices.clear();
        unexploredDataChoices.clear();
        choiceStep = null;
    }

    /**
     * Clear this choice
     */
    public void clear() {
        clearCurrent();
        clearUnexplored();
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        if (currentScheduleChoice != null) {
            sb.append(String.format("curr@%s", currentScheduleChoice));
        }
        if (currentDataChoice != null) {
            sb.append(String.format("curr:%s", currentDataChoice));
        }
        if (unexploredScheduleChoices != null && !unexploredScheduleChoices.isEmpty()) {
            sb.append(String.format(" rem@%s", unexploredScheduleChoices));
        }
        if (unexploredDataChoices != null && !unexploredDataChoices.isEmpty()) {
            sb.append(String.format(" rem:%s", unexploredDataChoices));
        }
        return sb.toString();
    }
}