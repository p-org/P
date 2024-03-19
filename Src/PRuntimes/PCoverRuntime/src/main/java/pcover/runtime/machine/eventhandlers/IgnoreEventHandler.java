package pcover.runtime.machine.eventhandlers;

import pcover.runtime.machine.PMachine;
import pcover.values.PEvent;
import pcover.values.PValue;

/**
 * Represents the ignore event handler
 */
public class IgnoreEventHandler extends EventHandler {

    /**
     * Constructor
     *
     * @param event Event
     */
    public IgnoreEventHandler(PEvent event) {
        super(event);
    }

    @Override
    public void handleEvent(PMachine target, PValue<?> payload) {
        // Ignore
    }
}
