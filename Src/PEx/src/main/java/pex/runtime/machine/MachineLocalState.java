package pex.runtime.machine;

import java.io.Serializable;
import java.util.List;
import java.util.Set;

/**
 * Represents the local state of a machine
 *
 * @param locals List of values of all local variables (including internal variables like currentState, FIFO queue, etc.)
 */
public record MachineLocalState(List<Object> locals, Set<String> observedEvents,
                                Set<String> happensBeforePairs) implements Serializable {
}
