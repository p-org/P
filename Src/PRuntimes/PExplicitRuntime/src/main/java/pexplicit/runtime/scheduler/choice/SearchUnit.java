package pexplicit.runtime.scheduler.choice;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;
import java.util.List;

/**
 * Represents a schedule or data choice
 */
public abstract class SearchUnit<T> implements Serializable {
    @Getter
    @Setter
    protected T current;
    @Getter
    @Setter
    protected List<T> unexplored;

    /**
     * Step number
     */
    @Getter
    protected int stepNumber = 0;
    /**
     * Choice number
     */
    @Getter
    protected int choiceNumber = 0;

    protected SearchUnit(T c, List<T> u, int stepNum, int choiceNum) {
        this.current = c;
        this.unexplored = u;
        this.stepNumber = stepNum;
        this.choiceNumber = choiceNum;
    }

    /**
     * Check if this choice has an unexplored choice remaining.
     *
     * @return true if this choice has an unexplored choice, false otherwise
     */
    public boolean isUnexploredNonEmpty() {
        return !unexplored.isEmpty();
    }

    /**
     * Clear current choices
     */
    public void clearCurrent() {
        this.current = null;
    }

    /**
     * Clean unexplored choices
     */
    abstract public void clearUnexplored();

    /**
     * Copy current choice as a new Choice object
     *
     * @return Choice object with the copied current choice
     */
    abstract public SearchUnit copyCurrent();

    /**
     * Copy this choice to a new choice and clear any unexplored choices.
     *
     * @return New choice same as original choice
     */
    abstract public SearchUnit transferChoice();

    abstract public String toString();
}