package pcover.runtime.machine.eventhandlers;


import pcover.runtime.machine.Machine;
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
    public void handleEvent(Machine target, PValue<?> payload) {
        throw new NotImplementedException();
    }
}