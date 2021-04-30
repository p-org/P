package psymbolic.runtime;

import psymbolic.valuesummary.UnionVS;
import psymbolic.valuesummary.bdd.Bdd;

public class PushEventHandler extends EventHandler {
    public final State dest;

    public PushEventHandler(EventName eventName, State dest) {
        super(eventName);
        this.dest = dest;
    }

    @Override
    public void handleEvent(Bdd pc, UnionVS payload, Machine machine, Outcome outcome) {
        outcome.addGuardedPush(pc, dest, payload);
    }
}
