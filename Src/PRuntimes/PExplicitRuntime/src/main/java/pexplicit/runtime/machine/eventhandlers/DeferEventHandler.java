package pexplicit.runtime.machine.eventhandlers;


import pexplicit.runtime.machine.PMachine;
import pexplicit.utils.exceptions.NotImplementedException;
import pexplicit.values.PEvent;
import pexplicit.values.PValue;

/**
 * Represents a defer event handler
 */
public class DeferEventHandler extends EventHandler {

    /**
     * Constructor
     *
     * @param event Event to defer
     */
    public DeferEventHandler(PEvent event) {
        super(event);
    }

    /**
     * TODO
     */
    @Override
    public void handleEvent(PMachine target, PValue<?> payload) {
        throw new NotImplementedException();
    }
}