package pcontainment.runtime.machine.eventhandlers;

import pcontainment.runtime.Event;
import pcontainment.runtime.machine.Machine;
import pcontainment.runtime.Message;
import pcontainment.valuesummary.Guard;
import pcontainment.valuesummary.PrimitiveVS;
import pcontainment.valuesummary.UnionVS;

public class DeferEventHandler extends EventHandler {

    public DeferEventHandler(Event event) {
        super(event);
    }

    @Override
    public void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason outcome) {
        Message deferredMessage =  new Message(event, new PrimitiveVS<>(), payload);
        target.deferredQueue.defer(pc,deferredMessage.restrict(pc));
    }
}