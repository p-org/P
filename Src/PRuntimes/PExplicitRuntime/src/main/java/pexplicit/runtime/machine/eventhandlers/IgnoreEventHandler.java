package pexplicit.runtime.machine.eventhandlers;

import pexplicit.runtime.machine.PMachine;
import pexplicit.values.PEvent;
import pexplicit.values.PValue;

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
