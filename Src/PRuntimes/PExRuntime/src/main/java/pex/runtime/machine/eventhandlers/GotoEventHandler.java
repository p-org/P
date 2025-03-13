package pex.runtime.machine.eventhandlers;

import pex.runtime.machine.PMachine;
import pex.runtime.machine.State;
import pex.values.Event;
import pex.values.PValue;

/**
 * Represents the goto event handler
 */
public class GotoEventHandler extends EventHandler {
    public final State gotoState;

    /**
     * Constructor
     *
     * @param event Event
     * @param dest  Destination state
     */
    public GotoEventHandler(Event event, State dest) {
        super(event);
        this.gotoState = dest;
    }

    public void transitionFunction(PMachine target, PValue<?> payload) {
    }

    /**
     * Handle the goto event at the target machine.
     *
     * @param target  Target machine on which the state transition is to be performed.
     * @param payload Payload associated with the goto state entry function.
     */
    @Override
    public void handleEvent(PMachine target, PValue<?> payload) {
        transitionFunction(target, payload);
        target.processStateTransition(gotoState, payload);
    }
}
