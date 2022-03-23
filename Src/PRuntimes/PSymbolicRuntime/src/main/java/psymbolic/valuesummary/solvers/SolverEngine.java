package psymbolic.valuesummary.solvers;

import lombok.Getter;
import lombok.Setter;
import psymbolic.commandline.EntryPoint;
import psymbolic.commandline.MemoutException;
import psymbolic.commandline.TimeoutException;
import psymbolic.valuesummary.solvers.bdd.PJBDDImpl;
import psymbolic.valuesummary.solvers.sat.SatExpr;
import psymbolic.valuesummary.solvers.sat.expr.ExprLibType;
import psymbolic.valuesummary.solvers.sat.SatGuard;

import java.time.Duration;
import java.time.Instant;

/**
 * Represents the generic backend engine
 */
public class SolverEngine {
    @Getter @Setter
    private static SolverLib solver;
    @Getter @Setter
    private static SolverType solverType = SolverType.BDD;
    @Getter @Setter
    private static ExprLibType exprLibType = ExprLibType.Auto;

    @Getter @Setter
    // time limit in seconds (0 means infinite)
    private static double timeLimit = 0;
    @Getter @Setter
    // memory limit in megabytes (0 means infinite)
    private static double memLimit = 0;
    @Getter @Setter
    // max memory in megabytes
    private static double maxMemSpent = 0;

    public static double checkForTimeout() {
        Instant end = Instant.now();
        double timeSpent = (Duration.between(EntryPoint.start, end).toMillis() / 1000.0);
        if (timeLimit > 0) {
            if (timeSpent > timeLimit) {
                throw new TimeoutException(String.format("Max time limit reached: %.1f seconds", timeSpent), timeSpent);
            }
        }
        return timeSpent;
    }

    public static double checkForMemout() {
        Runtime runtime = Runtime.getRuntime();
        double memSpent = (runtime.totalMemory() - runtime.freeMemory()) / 1000000.0;
        if (maxMemSpent < memSpent)
            maxMemSpent = memSpent;
        if (memLimit > 0) {
            if (memSpent > memLimit) {
                throw new MemoutException(String.format("Max memory limit reached: %.1f MB", memSpent), memSpent);
            }
        }
        return memSpent;
    }

    public static void simplifyEngineAuto() {
        switch (getExprLibType()) {
            case Iaig:
                simplifyEngine();
                break;
        }
    }

    private static void simplifyEngine() {
//        System.out.println("Simplifying solver engine: "
//                + getSolverType().toString() + " + "
//                + getExprLibType().toString());
        SatExpr.startSimplify();
        SolverGuard.simplifySolverGuard();
        SatExpr.stopSimplify();
//        System.out.println("\tDone");
    }

    public static void switchEngineAuto() {
        if (getExprLibType() != ExprLibType.Auto)
            return;
        switch (getSolverType()) {
            case BDD:
            case CBDD:
                if ((memLimit > 0) && (SolverEngine.checkForMemout() > (0.8*memLimit))) {
//                if (SolverEngine.getSolver().getNodeCount() > 20000000) {
                    switchEngine(SolverType.YICES2, ExprLibType.Fraig);
                    SolverEngine.cleanupEngine();
                    System.gc();
                }
                break;
        }
    }

    private static void switchEngine(SolverType type, ExprLibType etype) {
        if (type == getSolverType() && etype == getExprLibType())
            return;
        System.out.println("Switching solver engine:\n\t"
                            + getSolverType().toString() + "\t-> " + type.toString() + "\n\t"
                            + getExprLibType().toString() + "\t-> " + etype.toString());
        setSolver(type, etype);
        SolverGuard.switchSolverGuard();
    }

    public static void resetEngine(SolverType type, ExprLibType etype) {
        System.out.println("Resetting solver engine to "
                + type.toString() + " + "
                + etype.toString());
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
