package pcontainment.runtime.machine;

import com.microsoft.z3.BoolExpr;
import lombok.Getter;
import pcontainment.Checker;
import pcontainment.Pair;
import pcontainment.Triple;
import pcontainment.runtime.Payloads;
import pcontainment.runtime.machine.eventhandlers.EventHandlerReturnReason;

import java.util.HashMap;
import java.util.Map;

public abstract class State {
    private static int stateCount = 0;
    @Getter
    private final int id;
    private final String name;
    public Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> getEntryEncoding(int sends, Checker c,
                                                                                             Locals locals,
                                                                                             Machine machine,
                                                                                             Payloads payloads )
    {
        Map<BoolExpr, Triple<Integer, Locals, EventHandlerReturnReason>> ret = new HashMap<>();
        ret.put(c.mkBool(true), new Triple<>(sends, locals, new EventHandlerReturnReason.NormalReturn()));
        return ret;
    }
    public Map<BoolExpr, Pair<Integer, Locals>> getExitEncoding (int sends, Locals locals, Checker c, Machine machine) {
        Map<BoolExpr, Pair<Integer, Locals>> ret = new HashMap<>();
        ret.put(c.mkBool(true), new Pair<>(sends, locals));
        return ret;
    }

    public State(String name) {
        this.name = name;
        this.id = stateCount++;
    }

    @Override
    public String toString() {
        return String.format("%s", name);
    }
}
