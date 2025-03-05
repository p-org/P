package pex.runtime.machine.buffer;

import pex.runtime.machine.PMachine;

import java.io.Serializable;

/**
 * Represents the defer queue at the target machine to track deferred events
 */
public class DeferQueue extends MessageQueue implements Serializable {

    public DeferQueue(PMachine owner) {
        super(owner);
    }
}
