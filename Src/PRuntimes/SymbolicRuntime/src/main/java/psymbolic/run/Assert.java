package psymbolic.run;

import psymbolic.runtime.RuntimeLogger;
import psymbolic.runtime.Scheduler;
import psymbolic.valuesummary.PrimVS;
import psymbolic.valuesummary.bdd.Bdd;

import java.util.List;
import java.util.stream.Collectors;

public class Assert {

    public static void prop(boolean p, Scheduler scheduler, Bdd pc) {
        prop(p, "", scheduler, pc);
    }

    public static void prop(boolean p, String msg, Scheduler scheduler, Bdd pc) {
        if (!p) {
            RuntimeLogger.enable();
            throw new BugFoundException("Property violated: " + msg, pc);
        }
    }
    public static void progProp(boolean p, PrimVS<String> msg, Scheduler scheduler, Bdd pc) {
        if (!p) {
            RuntimeLogger.enable();
            List<String> msgs = msg.guard(pc).getGuardedValues().stream().map(x -> x.value).collect(Collectors.toList());
            throw new BugFoundException("Properties violated: " + msgs, pc);
        }
    }

}
