package psym.runtime.machine.eventhandlers;

import psym.runtime.machine.Machine;
import psym.runtime.machine.events.Event;
import psym.valuesummary.Guard;
import psym.valuesummary.UnionVS;

public class IgnoreEventHandler extends EventHandler {

    public IgnoreEventHandler(Event event) {
        super(event);
    }

    @Override
    public void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason outcome) {
        // Ignore
    }
}
