package pexplicit.runtime.machine.buffer;

import pexplicit.runtime.machine.PMachine;
import pexplicit.utils.exceptions.NotImplementedException;
import pexplicit.values.PEvent;
import pexplicit.values.PValue;

import java.io.Serializable;

/**
 * Represents a FIFO sender event queue
 */
public class SenderQueue extends MessageQueue implements EventBuffer, Serializable {

    /**
     * Constructor
     *
     * @param owner Sender machine (owner of the queue)
     */
    public SenderQueue(PMachine owner) {
        super(owner);
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
