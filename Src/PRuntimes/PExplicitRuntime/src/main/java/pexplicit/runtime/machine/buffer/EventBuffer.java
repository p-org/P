package pexplicit.runtime.machine.buffer;

import pexplicit.runtime.machine.PMachine;
import pexplicit.values.PEvent;
import pexplicit.values.PValue;

/**
 * Represents an interface implemented by a machine event buffer
 */
public interface EventBuffer {
    /**
     * Send an event
     *
     * @param target  Target machine
     * @param event   Event
     * @param payload Event payload
     */
    void send(PMachine target, PEvent event, PValue<?> payload);
}
