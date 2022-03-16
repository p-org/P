package psymbolic.valuesummary.solvers;

import psymbolic.valuesummary.solvers.bdd.PJBDDImpl;
import psymbolic.valuesummary.solvers.sat.expr.ExprLibType;
import psymbolic.valuesummary.solvers.sat.SatGuard;

/**
 * Represents the generic backend engine
 */
public class SolverEngine {
    private static SolverEngine solverEngine;
    private static SolverLib solverImpl;
    private static SolverType solverType;
    private static ExprLibType exprLibType;

	// Make this constructor static so that the class cannot be instantiated
    public SolverEngine() {
    	setSolver(SolverType.BDD, ExprLibType.None);
    }

    public static SolverEngine getEngine() {
        return solverEngine;
    }
    
    public static SolverLib getSolver() {
        return solverImpl;
    }
    
    public static void resetEngine() {
    	resetEngine(solverType, exprLibType);
    }

    public static void resetEngine(SolverType type, ExprLibType etype) {
    	setSolver(type, etype);
    }

    public static void cleanupEngine() {
        solverImpl.cleanup();
    }
    
    public static void setSolver(SolverType type, ExprLibType etype) {
    	solverType = type;
        exprLibType = etype;
    	switch(type) {
    	case BDD:		solverImpl = new PJBDDImpl(false);
    		break;
    	case CBDD:		solverImpl = new PJBDDImpl(true);
    		break;
        default:        solverImpl = new SatGuard(type, etype);

    	}
    }

    public static int getVarCount() {
        return solverImpl.getVarCount();
    }

    public static int getGuardCount() {
        return SolverGuard.getGuardCount();
//        return solverImpl.getVarCount();
    }

    public static String getStats() {
        return solverImpl.getStats();
    }

}
