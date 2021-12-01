package psymbolic.runtime.machine.eventhandlers;

import psymbolic.runtime.Event;
import psymbolic.runtime.machine.Machine;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.UnionVS;

public class IgnoreEventHandler extends EventHandler {

    public IgnoreEventHandler(Event event) {
        super(event);
    }

    @Override
    public void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason outcome) {
        // Ignore
    }
}
