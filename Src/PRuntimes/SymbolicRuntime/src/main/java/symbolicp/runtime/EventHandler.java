package symbolicp.runtime;

import symbolicp.bdd.Bdd;
import symbolicp.vs.PrimVS;
import symbolicp.vs.UnionVS;
import symbolicp.vs.ValueSummary;
import symbolicp.vs.VectorClockVS;

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
