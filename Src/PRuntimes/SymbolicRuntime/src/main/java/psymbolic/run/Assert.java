package psymbolic.run;

import psymbolic.runtime.RuntimeLogger;
import psymbolic.runtime.Scheduler;
import psymbolic.valuesummary.bdd.Bdd;
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

}
