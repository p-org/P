package pcover.runtime.machine.buffer;

import pcover.runtime.machine.PMachine;

import java.io.Serializable;

/**
 * Implements the defer queue used to keep track of the deferred events.
 */
public class DeferQueue extends MessageQueue implements Serializable {

    /**
     * Constructor
     *
     * @param owner Owner machine
     */
    public DeferQueue(PMachine owner) {
        super(owner);
    }
}
