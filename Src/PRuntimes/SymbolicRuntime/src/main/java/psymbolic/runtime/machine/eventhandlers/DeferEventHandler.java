package psymbolic.runtime.machine.eventhandlers;

import psymbolic.runtime.EventName;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.Message;
import psymbolic.valuesummary.PrimitiveVS;
import psymbolic.valuesummary.UnionVS;
import psymbolic.valuesummary.Guard;

public class DeferEventHandler extends EventHandler {

    public DeferEventHandler(EventName event) {
        super(event);
    }

    @Override
    public void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason outcome) {
        Message deferredMessage =  new Message(event, new PrimitiveVS<>(), payload);
        target.deferredQueue.defer(pc,deferredMessage.guard(pc));
    }
}