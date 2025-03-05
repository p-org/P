package pex.runtime.machine.eventhandlers;

import pex.runtime.machine.PMachine;
import pex.values.PEvent;
import pex.values.PValue;

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
