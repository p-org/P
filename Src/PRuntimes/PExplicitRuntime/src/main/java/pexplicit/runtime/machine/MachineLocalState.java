package pexplicit.runtime.machine;

import lombok.Getter;
import org.apache.commons.lang3.tuple.ImmutablePair;
import pexplicit.values.PEvent;

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
    private final Set<PEvent> observedEvents;
    private final Set<ImmutablePair<PEvent, PEvent>> happensBeforePairs;
    private final int timelineHash;

    public MachineLocalState(List<Object> locals, Set<PEvent> observedEvents, Set<ImmutablePair<PEvent, PEvent>> happensBeforePairs) {
        this.locals = locals;
        this.observedEvents = observedEvents;
        this.happensBeforePairs = happensBeforePairs;
        this.timelineHash = happensBeforePairs.hashCode();
    }
}
