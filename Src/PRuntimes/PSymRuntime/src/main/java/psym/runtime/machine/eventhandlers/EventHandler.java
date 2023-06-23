package psym.runtime.machine.eventhandlers;

import psym.runtime.machine.events.Event;
import psym.runtime.machine.Machine;
import psym.valuesummary.Guard;
import psym.valuesummary.UnionVS;

import java.io.Serializable;

public abstract class EventHandler implements Serializable {
    public final Event event;

    protected EventHandler(Event eventName) {
        this.event = eventName;
    }

    public abstract void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason outcome);
}
