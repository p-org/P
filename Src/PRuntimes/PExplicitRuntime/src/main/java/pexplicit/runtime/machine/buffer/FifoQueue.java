package pexplicit.runtime.machine.buffer;

import pexplicit.runtime.machine.PMachine;

import java.io.Serializable;

/**
 * Represents a FIFO event queue
 */
public class FifoQueue extends MessageQueue implements Serializable {

    /**
     * Constructor
     *
     * @param owner Machine (owner of the queue)
     */
    public FifoQueue(PMachine owner) {
        super(owner);
    }
}
