package pcontainment.runtime.machine.eventhandlers;

import com.microsoft.z3.BoolExpr;
import pcontainment.Pair;
import pcontainment.Triple;
import pcontainment.runtime.Event;
import pcontainment.runtime.Payloads;
import pcontainment.runtime.machine.Locals;
import pcontainment.runtime.machine.Machine;
import pcontainment.runtime.machine.State;

import java.util.HashMap;
import java.util.Map;

public class IgnoreEventHandler extends EventHandler {

    public IgnoreEventHandler(Event event, State state) {
        super(event, state);
    }

    @Override
    public Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>>
        getEncoding(int sends, Locals locals, Machine target, Payloads payloads) {
            return new HashMap<>();
        }
}
