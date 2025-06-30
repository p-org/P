package pex.runtime.machine.buffer;

import pex.runtime.machine.PMachine;
import pex.utils.exceptions.NotImplementedException;
import pex.values.Event;
import pex.values.PValue;

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
    public void send(PMachine target, Event eventName, PValue<?> payload) {
        throw new NotImplementedException();
    }

}
