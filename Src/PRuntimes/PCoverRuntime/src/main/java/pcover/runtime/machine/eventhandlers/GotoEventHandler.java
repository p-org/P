package pcover.runtime.machine.eventhandlers;

import pcover.runtime.machine.PMachine;
import pcover.runtime.machine.State;
import pcover.utils.exceptions.NotImplementedException;
import pcover.values.PEvent;
import pcover.values.PValue;

/**
 * Represents the goto event handler
 */
public class GotoEventHandler extends EventHandler {
    public final State gotoState;

    /**
     * Constructor
     * @param event Event
     * @param dest Destination state
     */
    public GotoEventHandler(PEvent event, State dest) {
        super(event);
        this.gotoState = dest;
    }

    /**
     * TODO
     */
    @Override
    public void handleEvent(PMachine target, PValue<?> payload) {
        throw new NotImplementedException();
    }
}
