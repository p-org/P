package psymbolic.runtime.eventhandlers;

import psymbolic.runtime.EventHandler;
import psymbolic.runtime.EventName;
import psymbolic.runtime.Machine;
import psymbolic.runtime.Outcome;
import psymbolic.valuesummary.UnionVS;
import psymbolic.valuesummary.bdd.Bdd;

public class DeferEventHandler extends EventHandler {

    public DeferEventHandler(EventName eventName) {
        super(eventName);
    }

    @Override
    public void handleEvent(Bdd pc, UnionVS payload, Machine machine, Outcome outcome) {
        machine.deferredQueue.defer(pc, makeEvent(payload, machine.getClock()).guard(pc));
    }
}