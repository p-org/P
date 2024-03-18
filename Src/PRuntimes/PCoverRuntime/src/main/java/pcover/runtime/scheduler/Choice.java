package pcover.runtime.scheduler;

import lombok.Getter;
import lombok.Setter;
import pcover.runtime.machine.PMachine;
import pcover.values.PValue;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.List;

/**
 * Represents a schedule or data choice
 */
public class Choice implements Serializable {
    @Getter @Setter
    PMachine repeatScheduleChoice = null;
    @Getter @Setter PValue<?> repeatDataChoice = null;
    @Getter List<PMachine> backtrackScheduleChoice = new ArrayList<>();
    @Getter List<PValue<?>> backtrackDataChoice = new ArrayList<>();

    @Getter int schedulerDepth = 0;
    @Getter int schedulerChoiceDepth = 0;

    /**
     * Constructor
     */
    public Choice() {}

    /**
     * Copy-constructor for Choice
     * @param old The choice to copy
     */
    private Choice(Choice old) {
        repeatScheduleChoice = old.repeatScheduleChoice;
        repeatDataChoice = old.repeatDataChoice;
        backtrackScheduleChoice = new ArrayList<>(old.backtrackScheduleChoice);
        backtrackDataChoice = new ArrayList<>(old.backtrackDataChoice);
        schedulerDepth = old.schedulerDepth;
        schedulerChoiceDepth = old.schedulerChoiceDepth;
    }

    /**
     * Copy the Choice
     * @return A new cloned copy of the Choice
     */
    public Choice getCopy() {
        return new Choice(this);
    }

    /**
     * Check if this choice has a backtrack choice remaining.
     * @return true if this choice has a backtrack choice, false otherwise
     */
    public boolean isBacktrackNonEmpty() {
        return isScheduleBacktrackNonEmpty() || isDataBacktrackNonEmpty();
    }

    /**
     * Check if this choice has a backtrack schedule choice remaining.
     * @return true if this choice has a backtrack schedule choice, false otherwise
     */
    public boolean isScheduleBacktrackNonEmpty() {
        return !getBacktrackScheduleChoice().isEmpty();
    }

    /**
     * Check if this choice has a backtrack data choice remaining.
     * @return true if this choice has a backtrack data choice, false otherwise
     */
    public boolean isDataBacktrackNonEmpty() {
        return !getBacktrackDataChoice().isEmpty();
    }

    /**
     * Add a backtrack schedule choice to this choice.
     * @param choice Machine to add as schedule choice
     */
    public void addBacktrackScheduleChoice(PMachine choice) {
        backtrackScheduleChoice.add(choice);
    }

    /**
     * Add a backtrack data choice to this choice.
     * @param choice PValue to add as data choice
     */
    public void addBacktrackDataChoice(PValue<?> choice) {
        backtrackDataChoice.add(choice);
    }

    /**
     * Clear repeat choices
     */
    public void clearRepeat() {
        repeatScheduleChoice = null;
        repeatDataChoice = null;
    }

    /**
     * Clean backtrack choices
     */
    public void clearBacktrack() {
        backtrackScheduleChoice.clear();
        backtrackDataChoice.clear();
    }

    /**
     * Clear this choice
     */
    public void clear() {
        clearRepeat();
        clearBacktrack();
    }
}