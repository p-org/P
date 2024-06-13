package pexplicit.runtime.scheduler.choice;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;
import java.util.List;

/**
 * Represents a schedule or data search unit
 */
public abstract class SearchUnit<T> implements Serializable {
    @Getter
    @Setter
    protected List<T> unexplored;

    protected SearchUnit(List<T> u) {
        this.unexplored = u;
    }

    /**
     * Clean unexplored choices
     */
    public void clearUnexplored() {
        unexplored.clear();
    }

    public abstract SearchUnit transferUnit();
}

