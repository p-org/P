package psymbolic.runtime.statistics;

import psymbolic.valuesummary.solvers.SolverEngine;

public class SolverStats {
    public static int andOperations = 0;
    public static int orOperations = 0;
    public static int notOperations = 0;
    public static int isSatOperations = 0;
    public static int isSatResult = 0;

    public static double isSatPercent() {
    	return (isSatOperations == 0 ? 0.0 : (isSatResult * 100.0 / isSatOperations));
    }
    
    public static String prettyPrint() {
        return    String.format(   "  solver-#-vars:\t%d", SolverEngine.getVarCount())
                + String.format( "\n  solver-#-guards:\t%d", SolverEngine.getGuardCount())
                + String.format( "\n  solver-#-nodes:\t%d", SolverEngine.getSolver().getNodeCount())
                + String.format( "\n  solver-#-and-ops:\t%d", andOperations)
        		+ String.format( "\n  solver-#-or-ops:\t%d", orOperations)
        		+ String.format( "\n  solver-#-not-ops:\t%d", notOperations)
        		+ String.format( "\n  solver-#-sat-ops:\t%d", isSatOperations)
                + String.format( "\n  solver-#-sat-ops-sat:\t%d", isSatResult)
                + String.format( "\n  solver-%%-sat-ops-sat:\t%.1f", isSatPercent());
    }
}
