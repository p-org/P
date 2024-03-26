package pexplicit.runtime.machine.buffer;

import pexplicit.runtime.machine.PMachine;
import pexplicit.utils.exceptions.NotImplementedException;
import pexplicit.values.PEvent;
import pexplicit.values.PValue;

import java.io.Serializable;

/**
 * Represents a FIFO event queue
 */
public class FifoQueue extends MessageQueue implements EventBuffer, Serializable {

    private final PMachine sender;

    /**
     * Constructor
     *
     * @param sender Sender machine (owner of the queue)
     */
    public FifoQueue(PMachine sender) {
        super(sender);
        this.sender = sender;
    }

    /**
     * TODO
     *
     * @param target    Target machine
     * @param eventName Event
     * @param payload   Event payload
     */
    public void send(PMachine target, PEvent eventName, PValue<?> payload) {
        throw new NotImplementedException();
    }

}
