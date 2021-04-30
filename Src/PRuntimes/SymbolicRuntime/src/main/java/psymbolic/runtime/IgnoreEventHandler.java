package psymbolic.runtime;

import psymbolic.valuesummary.bdd.Bdd;
import psymbolic.valuesummary.*;

public class IgnoreEventHandler extends EventHandler {

    public IgnoreEventHandler(EventName eventName) {
        super(eventName);
    }

    @Override
    public void handleEvent(Bdd pc, UnionVS payload, Machine machine, Outcome outcome) {
        // Ignore
    }
}
