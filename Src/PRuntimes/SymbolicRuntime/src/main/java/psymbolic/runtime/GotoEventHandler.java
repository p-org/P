package psymbolic.runtime;

import psymbolic.valuesummary.bdd.Bdd;
import psymbolic.valuesummary.*;

public class GotoEventHandler extends EventHandler {
    public final State dest;

    public GotoEventHandler(EventName eventName, State dest) {
        super(eventName);
        this.dest = dest;
    }

    public void transitionAction(Bdd pc, Machine machine, UnionVS payload) {}

    @Override
    public void handleEvent(Bdd pc, UnionVS payload, Machine machine, Outcome outcome) {
        transitionAction(pc, machine, payload);
        outcome.addGuardedGoto(pc, dest, payload);
    }
}
