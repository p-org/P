package pex.runtime.machine.eventhandlers;


import pex.runtime.machine.PMachine;
import pex.utils.exceptions.NotImplementedException;
import pex.values.PEvent;
import pex.values.PValue;

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