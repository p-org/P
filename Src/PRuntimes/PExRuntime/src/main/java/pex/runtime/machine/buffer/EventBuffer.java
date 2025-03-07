package pex.runtime.machine.buffer;

import pex.runtime.machine.PMachine;
import pex.values.PEvent;
import pex.values.PValue;

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
