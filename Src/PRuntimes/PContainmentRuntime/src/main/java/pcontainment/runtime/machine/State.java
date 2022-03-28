package pcontainment.runtime.machine;

import com.microsoft.z3.BoolExpr;
import lombok.Getter;
import pcontainment.Checker;
import pcontainment.Pair;
import pcontainment.runtime.machine.eventhandlers.EventHandlerReturnReason;

import java.util.HashMap;
import java.util.Map;

public abstract class State {
    private static int stateCount = 0;
    @Getter
    private final int id;
    private final String name;
    public Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> getEntryEncoding(int sends, Checker c, Machine machine ) {
        Map<BoolExpr, Pair<Integer, EventHandlerReturnReason>> ret = new HashMap<>();
        ret.put(c.mkBool(true), new Pair<>(sends, new EventHandlerReturnReason.NormalReturn()));
        return ret;
    }
    public Map<BoolExpr, Integer> getExitEncoding (int sends, Checker c, Machine machine) {
        Map<BoolExpr, Integer> ret = new HashMap<>();
        ret.put(c.mkBool(true), sends);
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
