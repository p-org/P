package pex.runtime.scheduler.choice;

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

    protected Choice(T c) {
        this.current = c;
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
    abstract public Choice copyCurrent(boolean copyState);

    abstract public String toString();
}