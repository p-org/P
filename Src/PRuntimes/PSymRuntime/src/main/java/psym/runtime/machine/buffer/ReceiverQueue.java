package psym.runtime.machine.buffer;

import psym.runtime.machine.events.Message;
import psym.valuesummary.Guard;

import java.io.Serializable;

/** Implements the Receiver Queue used to keep track of events with receiver queue semantics */
public class ReceiverQueue extends SymbolicQueue implements Serializable {

    public ReceiverQueue() {
        super();
    }

    /**
     * Receive a particular event by adding it to the receiver queue of the machine
     *
     * @param pc Guard under which to defer the event
     * @param event Event to be deferred
     */
    public void receive(Guard pc, Message event) {
        add(event.restrict(pc));
    }
}

