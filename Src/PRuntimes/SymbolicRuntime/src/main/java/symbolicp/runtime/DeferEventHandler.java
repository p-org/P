package symbolicp.runtime;

import symbolicp.bdd.Bdd;
import symbolicp.vs.PrimVS;
import symbolicp.vs.UnionVS;
import symbolicp.vs.ValueSummary;

public class DeferEventHandler extends EventHandler {

    public DeferEventHandler(EventName eventName) {
        super(eventName);
    }

    @Override
    public void handleEvent(Bdd pc, UnionVS payload, Machine machine, Outcome outcome) {
        machine.deferredQueue.defer(pc, makeEvent(payload, machine.getClock()).guard(pc));
    }
}