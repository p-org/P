package pexplicit.runtime.machine;

import lombok.Getter;

import java.io.Serializable;
import java.util.List;
import java.util.Set;

/**
 * Represents the local state of a machine
 */
@Getter
public class MachineLocalState implements Serializable {
    /**
     * List of values of all local variables (including internal variables like currentState, FIFO queue, etc.)
     */
    private final List<Object> locals;
    private final Set<String> observedEvents;
    private final Set<String> happensBeforePairs;

    public MachineLocalState(List<Object> locals, Set<String> observedEvents, Set<String> happensBeforePairs) {
        this.locals = locals;
        this.observedEvents = observedEvents;
        this.happensBeforePairs = happensBeforePairs;
    }
}
