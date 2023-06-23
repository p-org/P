package psym.runtime.machine.eventhandlers;

import psym.runtime.machine.Machine;
import psym.runtime.machine.State;
import psym.runtime.machine.events.Event;
import psym.valuesummary.Guard;
import psym.valuesummary.UnionVS;

public class GotoEventHandler extends EventHandler {
    public final State gotoState;

    public GotoEventHandler(Event event, State dest) {
        super(event);
        this.gotoState = dest;
    }

    public void transitionFunction(Guard pc, Machine machine, UnionVS payload) {
    }

    @Override
    public void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason outcome) {
        transitionFunction(pc, target, payload);
        outcome.addGuardedGoto(pc, gotoState, payload);
    }
}
