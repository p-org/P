package pcontainment.runtime.machine.eventhandlers;

import com.microsoft.z3.BoolExpr;
import pcontainment.Pair;
import pcontainment.runtime.Event;
import pcontainment.runtime.Payloads;
import pcontainment.runtime.machine.Machine;

import java.util.HashMap;
import java.util.Map;

public class IgnoreEventHandler extends EventHandler {

    public IgnoreEventHandler(Event event) {
        super(event);
    }

    @Override
    public Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> getEncoding(int sends, Machine target, Payloads payloads) {
        return new HashMap<>();
    }
}
