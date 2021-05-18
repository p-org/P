package psymbolic.runtime.machine.eventhandlers;

import psymbolic.runtime.Event;
import psymbolic.runtime.machine.Machine;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.UnionVS;

public abstract class EventHandler {
    public final Event event;

    protected EventHandler(Event eventName) {
        this.event = eventName;
    }

    public abstract void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason outcome);
}
