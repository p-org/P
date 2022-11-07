package psymbolic.valuesummary.solvers;

import lombok.Getter;
import lombok.Setter;
import psymbolic.runtime.logger.SearchLogger;
import psymbolic.runtime.statistics.SolverStats;
import psymbolic.utils.MemoryMonitor;
import psymbolic.valuesummary.solvers.bdd.PJBDDImpl;
import psymbolic.valuesummary.solvers.sat.SatExpr;
import psymbolic.valuesummary.solvers.sat.expr.ExprLibType;
import psymbolic.valuesummary.solvers.sat.SatGuard;

/**
 * Represents the generic backend engine
 */
public class SolverEngine {
    @Getter @Setter
    private static SolverLib solver;
    @Getter @Setter
    private static SolverType solverType = SolverType.BDD;
    @Getter @Setter
    private static ExprLibType exprLibType = ExprLibType.Bdd;

    public static void simplifyEngineAuto() {
        switch (getExprLibType()) {
            case Iaig:
                simplifyEngine();
                break;
        }
    }

    private static void simplifyEngine() {
//        SearchLogger.log("Simplifying solver engine: "
//                + getSolverType().toString() + " + "
//                + getExprLibType().toString());
        SatExpr.startSimplify();
        SolverGuard.simplifySolverGuard();
        SatExpr.stopSimplify();
//        SearchLogger.log("\tDone");
    }

    public static void switchEngineAuto() {
        if (getExprLibType() != ExprLibType.Auto)
            return;
        switch (getSolverType()) {
            case BDD:
            case CBDD:
                if ((SolverStats.memLimit > 0) && (MemoryMonitor.getMemSpent() > (0.8* SolverStats.memLimit))) {
//                if (SolverEngine.getSolver().getNodeCount() > 20000000) {
                    switchEngine(SolverType.YICES2, ExprLibType.Fraig);
                    SolverEngine.cleanupEngine();
                }
                break;
        }
    }

    private static void switchEngine(SolverType type, ExprLibType etype) {
        if (type == getSolverType() && etype == getExprLibType())
            return;
        SearchLogger.log("Switching solver engine:");
        SearchLogger.log(String.format("  %-20s->%-20s", getSolverType().toString(), type.toString()));
        SearchLogger.log(String.format("  %-20s->%-20s", getExprLibType().toString(), etype.toString()));
        setSolver(type, etype);
        SolverGuard.switchSolverGuard();
    }

    public static void resumeEngine() {
        SearchLogger.log("Resuming solver engine:");
        SearchLogger.log(String.format("  %-20s->%-20s", getSolverType().toString(), getExprLibType().toString()));
        setSolver(getSolverType(), getExprLibType());
        SolverGuard.resumeSolverGuard();
    }

    public static void resetEngine(SolverType type, ExprLibType etype) {
        if (SearchLogger.getVerbosity() > 1) {
            SearchLogger.log("Setting solver engine to "
                    + type.toString() + " + "
                    + etype.toString());
        }
    	setSolver(type, etype);
        SolverGuard.reset();
    }

    public static void cleanupEngine() {
        solver.cleanup();
    }
    
    public static void setSolver(SolverType type, ExprLibType etype) {
    	setSolverType(type);
        setExprLibType(etype);
    	switch(type) {
    	case BDD:		solver = new PJBDDImpl(false);
    		break;
    	case CBDD:		solver = new PJBDDImpl(true);
    		break;
        default:        solver = new SatGuard(type, etype);

    	}
    }

    public static int getVarCount() {
        return solver.getVarCount();
    }

    public static int getGuardCount() {
        return SolverGuard.getGuardCount();
//        return solverImpl.getVarCount();
    }

    public static String getStats() {
        return solver.getStats();
    }

}
