package pcontainment.runtime.machine.eventhandlers;

import com.microsoft.z3.BoolExpr;
import pcontainment.Pair;
import pcontainment.Triple;
import pcontainment.runtime.*;
import pcontainment.runtime.machine.Locals;
import pcontainment.runtime.machine.Machine;
import pcontainment.runtime.machine.State;

import java.util.HashMap;
import java.util.Map;


public class GotoEventHandler extends EventHandler {
    public final State gotoState;

    public GotoEventHandler(Event event, State src, State dest) {
        super(event, src);
        this.gotoState = dest;
    }

    // TODO: transition functions

    @Override
    public Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>>
        getEncoding(int sends, Locals locals, Machine target, Payloads payloads) {
            return new HashMap<>();
        }

}
