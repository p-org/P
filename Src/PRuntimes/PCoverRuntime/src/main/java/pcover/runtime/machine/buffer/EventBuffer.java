package pcover.runtime.machine.buffer;

import pcover.runtime.machine.Machine;
import pcover.values.PEvent;
import pcover.values.PValue;

/**
 * Represents an interface implemented by a machine event buffer
 */
public interface EventBuffer {
    /**
     * Send an event
     * @param target Target machine
     * @param event Event
     * @param payload Event payload
     */
    void send(Machine target, PEvent event, PValue<?> payload);
}
