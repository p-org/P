package psymbolic.runtime.statistics;

import lombok.Setter;
import psymbolic.commandline.EntryPoint;
import psymbolic.commandline.MemoutException;
import psymbolic.valuesummary.solvers.SolverEngine;
import psymbolic.valuesummary.solvers.sat.expr.Fraig;

import java.time.Duration;
import java.time.Instant;

public class SolverStats {
    public static int andOperations = 0;
    public static int orOperations = 0;
    public static int notOperations = 0;
    public static int isSatOperations = 0;
    public static int isSatResult = 0;

    @Setter
    public static double timeLimit = 0;                 // time limit in seconds (0 means infinite)
    @Setter
    public static double memLimit = 0;                  // memory limit in megabytes (0 means infinite)

    public static double maxMemSpent = 0;               // max memory in megabytes
    public static double timeTotalCreateGuards = 0;     // total time in milliseconds to create guards
    public static double timeMaxCreateGuards = 0;       // max time in milliseconds to create guards
    public static double timeTotalSolveGuards = 0;      // total time in milliseconds to solve guards
    public static double timeMaxSolveGuards = 0;        // max time in milliseconds to solve guards

    public static void updateCreateGuardTime(long timeSpent) {
        timeTotalCreateGuards += timeSpent;
        if (timeMaxCreateGuards < timeSpent)
            timeMaxCreateGuards = timeSpent;

        // switch engine
        SolverEngine.switchEngineAuto();
        // check if reached time or memory limit
        checkResourceLimits();
    }

    public static void updateSolveGuardTime(long timeSpent) {
        timeTotalSolveGuards += timeSpent;
        if (timeMaxSolveGuards < timeSpent)
            timeMaxSolveGuards = timeSpent;
        // check if reached time or memory limit
        checkResourceLimits();
    }

    public static double getTime() {
        return (Duration.between(EntryPoint.start, Instant.now()).toMillis() / 1000.0);
    }

    public static double getMemory() {
        Runtime runtime = Runtime.getRuntime();
        double memSpent = (runtime.totalMemory() - runtime.freeMemory()) / 1000000.0;
        if (maxMemSpent < memSpent)
            maxMemSpent = memSpent;
        return memSpent;
    }

    public static void checkMemout(double memSpent) {
        if (memLimit > 0) {
            if (memSpent > memLimit) {
                throw new MemoutException(String.format("Max memory limit reached: %.1f MB", memSpent), memSpent);
            }
        }
    }

    public static void checkResourceLimits() {
        checkMemout(getMemory());
    }



    public static double getDoublePercent(double spent, double total) {
        return (spent == 0 ? 0.0 : (spent * 100.0 / total));
    }

    public static double isSatPercent(int isSatOps, int isSatRes) {
    	return (isSatOps == 0 ? 0.0 : (isSatRes * 100.0 / isSatOps));
    }
    
    public static String prettyPrint() {
        return    String.format(   "  #-vars:\t%d", SolverEngine.getVarCount())
                + String.format( "\n  #-guards:\t%d", SolverEngine.getGuardCount())
                + String.format( "\n  #-expr:\t%d", SolverEngine.getSolver().getExprCount())
                + String.format( "\n  #-and-ops:\t%d", andOperations)
        		+ String.format( "\n  #-or-ops:\t%d", orOperations)
        		+ String.format( "\n  #-not-ops:\t%d", notOperations)
                + String.format( "\n  aig-#-sat-ops:\t%d", Fraig.isSatOperations)
                + String.format( "\n  aig-#-sat-ops-sat:\t%d", Fraig.isSatResult)
                + String.format( "\n  aig-%%-sat-ops-sat:\t%.1f", isSatPercent(Fraig.isSatOperations, Fraig.isSatResult))
                + String.format( "\n  solver-#-nodes:\t%d", SolverEngine.getSolver().getNodeCount())
        		+ String.format( "\n  solver-#-sat-ops:\t%d", isSatOperations)
                + String.format( "\n  solver-#-sat-ops-sat:\t%d", isSatResult)
                + String.format( "\n  solver-%%-sat-ops-sat:\t%.1f", isSatPercent(isSatOperations, isSatResult));
    }
}
