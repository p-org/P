package pcontainment.runtime.machine.eventhandlers;

import com.microsoft.z3.BoolExpr;
import pcontainment.Pair;
import pcontainment.runtime.*;
import pcontainment.runtime.machine.Machine;
import pcontainment.runtime.machine.State;

import java.util.HashMap;
import java.util.Map;


public class GotoEventHandler extends EventHandler {
    public final State gotoState;

    public GotoEventHandler(Event event, State dest) {
        super(event);
        this.gotoState = dest;
    }

    // TODO: transition functions

    @Override
    public Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> getEncoding(int sends, Machine target, Payloads payloads) {
        return new HashMap<>();
    }

}
