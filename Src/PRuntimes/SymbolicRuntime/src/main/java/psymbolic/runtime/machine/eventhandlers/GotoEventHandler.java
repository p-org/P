package psymbolic.runtime.machine.eventhandlers;

import psymbolic.runtime.*;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.State;
import psymbolic.valuesummary.UnionVS;
import psymbolic.valuesummary.Guard;


public class GotoEventHandler extends EventHandler {
    public final State gotoState;

    public GotoEventHandler(EventName eventName, State dest) {
        super(eventName);
        this.gotoState = dest;
    }

    public void transitionFunction(Guard pc, Machine machine, UnionVS payload) {}

    @Override
    public void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason outcome) {
        transitionFunction(pc, target, payload);
        outcome.addGuardedGoto(pc, gotoState, payload);
    }
}
