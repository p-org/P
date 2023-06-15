package psym.commandline;

import psym.utils.BugFoundException;
import psym.utils.LivenessException;
import psym.valuesummary.Guard;
import psym.valuesummary.GuardedValue;
import psym.valuesummary.PrimitiveVS;

import java.util.List;
import java.util.stream.Collectors;

public class Assert {

    public static void prop(boolean p, String msg, Guard pc) {
        if (!p) {
            throw new BugFoundException("Property violated: " + msg, pc);
        }
    }

    public static void progProp(boolean p, PrimitiveVS<String> msg, Guard pc) {
        if (!p) {
            List<String> msgs = msg.restrict(pc).getGuardedValues().stream().map(GuardedValue::getValue).collect(Collectors.toList());
            throw new BugFoundException("Properties violated: " + msgs, pc);
        }
    }

    public static void liveness(boolean p, String msg, Guard pc) {
        if (!p) {
            throw new LivenessException("Property violated: " + msg, pc);
        }
    }

}
