package psymbolic.runtime.machine.eventhandlers;

import psymbolic.runtime.EventName;
import psymbolic.runtime.machine.Machine;
import psymbolic.valuesummary.UnionVS;
import psymbolic.valuesummary.Guard;

public class IgnoreEventHandler extends EventHandler {

    public IgnoreEventHandler(EventName event) {
        super(event);
    }

    @Override
    public void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason outcome) {
        // Ignore
    }
}
