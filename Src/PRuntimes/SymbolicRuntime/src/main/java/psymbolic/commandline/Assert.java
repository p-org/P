package psymbolic.commandline;

import psymbolic.runtime.scheduler.Scheduler;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.GuardedValue;
import psymbolic.valuesummary.PrimitiveVS;

import java.util.List;
import java.util.stream.Collectors;

public class Assert {

    public static void prop(boolean p, Scheduler scheduler, Guard pc) {
        prop(p, "", scheduler, pc);
    }

    public static void prop(boolean p, String msg, Scheduler scheduler, Guard pc) {
        if (!p) {
            throw new BugFoundException("Property violated: " + msg, pc);
        }
    }
    public static void progProp(boolean p, PrimitiveVS<String> msg, Scheduler scheduler, Guard pc) {
        if (!p) {
            List<String> msgs = msg.restrict(pc).getGuardedValues().stream().map(GuardedValue::getValue).collect(Collectors.toList());
            throw new BugFoundException("Properties violated: " + msgs, pc);
        }
    }

}
