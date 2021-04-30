package psymbolic.runtime;

import psymbolic.valuesummary.PrimVS;
import psymbolic.valuesummary.UnionVS;
import psymbolic.valuesummary.VectorClockVS;
import psymbolic.valuesummary.bdd.Bdd;

public abstract class EventHandler {
    public final EventName eventName;

    protected EventHandler(EventName eventName) {
        this.eventName = eventName;
    }

    public Event makeEvent(UnionVS payload, VectorClockVS clock) {
        return new Event(eventName, clock, new PrimVS<>(), payload);
    }

    public abstract void handleEvent(
        Bdd pc,
        UnionVS payload,
        Machine machine,
        Outcome outcome
    );
}
