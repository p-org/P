package pcover.runtime.machine.eventhandlers;


import pcover.runtime.machine.PMachine;
import pcover.utils.exceptions.NotImplementedException;
import pcover.values.PEvent;
import pcover.values.PValue;

/**
 * Represents a defer event handler
 */
public class DeferEventHandler extends EventHandler {

    /**
     * Constructor
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