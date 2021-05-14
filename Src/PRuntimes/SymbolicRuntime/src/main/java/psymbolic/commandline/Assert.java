package psymbolic.commandline;

import psymbolic.runtime.RuntimeLogger;
import psymbolic.runtime.Scheduler;
import psymbolic.valuesummary.Guard;

public class Assert {

    public static void prop(boolean p, Scheduler scheduler, Guard pc) {
        prop(p, "", scheduler, pc);
    }

    public static void prop(boolean p, String msg, Scheduler scheduler, Guard pc) {
        if (!p) {
            RuntimeLogger.enable();
            throw new BugFoundException("Property violated: " + msg, pc);
        }
    }

}
