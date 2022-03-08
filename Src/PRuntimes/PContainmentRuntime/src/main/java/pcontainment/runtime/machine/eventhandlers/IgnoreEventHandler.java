package pcontainment.runtime.machine.eventhandlers;

import pcontainment.runtime.Event;
import pcontainment.runtime.machine.Machine;
import pcontainment.valuesummary.Guard;
import pcontainment.valuesummary.UnionVS;

public class IgnoreEventHandler extends EventHandler {

    public IgnoreEventHandler(Event event) {
        super(event);
    }

    @Override
    public void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason outcome) {
        // Ignore
    }
}
