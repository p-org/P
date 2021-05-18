package psymbolic.runtime.machine.buffer;

import psymbolic.runtime.machine.Message;
import psymbolic.valuesummary.Guard;

/**
 * Implements the Defer Queue used to keep track of the deferred events
 */
public class DeferQueue extends SymbolicQueue<Message> {

    public DeferQueue() {
        super();
    }

    /**
     * Defer a particular event by adding it to the defer queue of the machine
     * @param pc Guard under which to defer the event
     * @param event Event to be deferred
     */
    public void defer(Guard pc, Message event) { enqueue(event.restrict(pc)); }
}
