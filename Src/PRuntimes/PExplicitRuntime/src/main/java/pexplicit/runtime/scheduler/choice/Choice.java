package pexplicit.runtime.scheduler.choice;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;

/**
 * Represents a schedule or data choice
 */
public abstract class Choice<T> implements Serializable {
    @Getter
    @Setter
    protected T current;

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

    protected Choice(T c, int stepNum, int choiceNum) {
        this.current = c;
        this.stepNumber = stepNum;
        this.choiceNumber = choiceNum;
    }

    /**
     * Clear current choices
     */
    public void clearCurrent() {
        this.current = null;
    }

    /**
     * Copy current choice as a new Choice object
     *
     * @return Choice object with the copied current choice
     */
    abstract public Choice copyCurrent();

    abstract public String toString();
}