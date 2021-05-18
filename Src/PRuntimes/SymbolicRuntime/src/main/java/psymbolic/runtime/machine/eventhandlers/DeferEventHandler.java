package psymbolic.runtime.machine.eventhandlers;

import psymbolic.runtime.Event;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.Message;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.PrimitiveVS;
import psymbolic.valuesummary.UnionVS;

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