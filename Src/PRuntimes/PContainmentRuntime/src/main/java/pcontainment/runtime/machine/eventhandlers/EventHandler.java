package pcontainment.runtime.machine.eventhandlers;

import pcontainment.runtime.Event;
import pcontainment.runtime.machine.Machine;
import pcontainment.valuesummary.Guard;
import pcontainment.valuesummary.UnionVS;

import java.util.Map;

public abstract class EventHandler {
    public final Event event;

    protected EventHandler(Event eventType) {
        this.event = eventType;
    }

    public abstract void handleEvent(Map<String, Object>);
}
