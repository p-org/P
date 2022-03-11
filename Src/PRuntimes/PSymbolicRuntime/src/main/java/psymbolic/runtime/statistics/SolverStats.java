package psymbolic.runtime.statistics;

import psymbolic.valuesummary.solvers.SolverEngine;
import psymbolic.valuesummary.solvers.sat.Aig;

public class SolverStats {
    public static int andOperations = 0;
    public static int orOperations = 0;
    public static int notOperations = 0;
    public static int isSatOperations = 0;
    public static int isSatResult = 0;

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
                + String.format( "\n  aig-#-sat-ops:\t%d", Aig.isSatOperations)
                + String.format( "\n  aig-#-sat-ops-sat:\t%d", Aig.isSatResult)
                + String.format( "\n  aig-%%-sat-ops-sat:\t%.1f", isSatPercent(Aig.isSatOperations, Aig.isSatResult))
                + String.format( "\n  solver-#-nodes:\t%d", SolverEngine.getSolver().getNodeCount())
        		+ String.format( "\n  solver-#-sat-ops:\t%d", isSatOperations)
                + String.format( "\n  solver-#-sat-ops-sat:\t%d", isSatResult)
                + String.format( "\n  solver-%%-sat-ops-sat:\t%.1f", isSatPercent(isSatOperations, isSatResult));
    }
}
