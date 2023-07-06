package psym.runtime.machine.buffer;

import psym.runtime.machine.Machine;

import java.io.Serializable;

/** Implements the Receiver Queue used to keep track of events with receiver queue semantics */
public class ReceiverQueue extends SymbolicQueue implements Serializable {

    public ReceiverQueue(Machine owner) {
        super(owner);
    }
}

