package pex.runtime.machine.eventhandlers;

import pex.runtime.machine.PMachine;
import pex.values.PEvent;
import pex.values.PValue;

import java.io.Serializable;

/**
 * Represents the base class for all event handlers
 */
public abstract class EventHandler implements Serializable {
    public final PEvent event;

    /**
     * Constructor
     *
     * @param event Event corresponding to the handler
     */
    protected EventHandler(PEvent event) {
        this.event = event;
    }

    /**
     * Defines what gets executed when handling this event handler
     *
     * @param target  Target machine on which the event is executed
     * @param payload Payload associated with the event
     */
    public abstract void handleEvent(PMachine target, PValue<?> payload);
}
