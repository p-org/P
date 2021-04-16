package symbolicp.runtime;

import symbolicp.bdd.Bdd;
import symbolicp.vs.UnionVS;
import symbolicp.vs.ValueSummary;

public class IgnoreEventHandler extends EventHandler {

    public IgnoreEventHandler(EventName eventName) {
        super(eventName);
    }

    @Override
    public void handleEvent(Bdd pc, UnionVS payload, Machine machine, Outcome outcome) {
        // Ignore
    }
}
