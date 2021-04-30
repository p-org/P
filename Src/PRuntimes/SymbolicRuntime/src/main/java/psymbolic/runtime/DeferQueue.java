package psymbolic.runtime;

import psymbolic.valuesummary.bdd.Bdd;

public class DeferQueue extends SymbolicQueue<Event> {

    public DeferQueue() {
        super();
    }

    public void defer(Bdd pc, Event event) {
        enqueueEntry(event.guard(pc));
    }
}
