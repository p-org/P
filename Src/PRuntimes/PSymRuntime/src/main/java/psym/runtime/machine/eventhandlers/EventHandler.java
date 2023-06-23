package psym.runtime.machine.eventhandlers;

import java.io.Serializable;
import psym.runtime.machine.Machine;
import psym.runtime.machine.events.Event;
import psym.valuesummary.Guard;
import psym.valuesummary.UnionVS;

public abstract class EventHandler implements Serializable {
    public final Event event;

    protected EventHandler(Event eventName) {
        this.event = eventName;
    }

    public abstract void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason outcome);
}
