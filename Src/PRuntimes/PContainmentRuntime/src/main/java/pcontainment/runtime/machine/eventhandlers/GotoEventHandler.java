package pcontainment.runtime.machine.eventhandlers;

import pcontainment.runtime.*;
import pcontainment.runtime.machine.Machine;
import pcontainment.runtime.machine.State;
import pcontainment.valuesummary.UnionVS;
import pcontainment.valuesummary.Guard;


public class GotoEventHandler extends EventHandler {
    public final State gotoState;

    public GotoEventHandler(Event event, State dest) {
        super(event);
        this.gotoState = dest;
    }

    public void transitionFunction(Guard pc, Machine machine, UnionVS payload) {}

    @Override
    public void handleEvent(Guard pc, Machine target, UnionVS payload, EventHandlerReturnReason outcome) {
        transitionFunction(pc, target, payload);
        outcome.addGuardedGoto(pc, gotoState, payload);
    }
}
