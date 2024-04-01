package pexplicit.runtime.scheduler;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.machine.PMachine;
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
    List<PMachine> unexploredScheduleChoice = new ArrayList<>();
    List<PValue<?>> unexploredDataChoice = new ArrayList<>();

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
        unexploredScheduleChoice = new ArrayList<>(old.unexploredScheduleChoice);
        unexploredDataChoice = new ArrayList<>(old.unexploredDataChoice);
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
        return !getUnexploredScheduleChoice().isEmpty();
    }

    /**
     * Check if this choice has an unexplored data choice remaining.
     *
     * @return true if this choice has an unexplored data choice, false otherwise
     */
    public boolean isUnexploredDataChoicesNonEmpty() {
        return !getUnexploredDataChoice().isEmpty();
    }

    /**
     * Add an unexplored schedule choice to this choice.
     *
     * @param choice Machine to add as schedule choice
     */
    public void addUnexploredScheduleChoice(PMachine choice) {
        unexploredScheduleChoice.add(choice);
    }

    /**
     * Add an unexplored data choice to this choice.
     *
     * @param choice PValue to add as data choice
     */
    public void addUnexploredDataChoice(PValue<?> choice) {
        unexploredDataChoice.add(choice);
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
        unexploredScheduleChoice.clear();
        unexploredDataChoice.clear();
    }

    /**
     * Clear this choice
     */
    public void clear() {
        clearCurrent();
        clearUnexplored();
    }
}