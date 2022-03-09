package psymbolic.valuesummary.solvers;

import psymbolic.valuesummary.solvers.bdd.PJBDDImpl;
import psymbolic.valuesummary.solvers.sat.SatGuard;
import psymbolic.valuesummary.solvers.sat.Aig;

/**
 * Represents the generic backend engine
 */
public class SolverEngine {
    private static SolverEngine solverEngine;
    private static SolverLib solverImpl;
    private static SolverType solverType;
	
	// Make this constructor static so that the class cannot be instantiated
    public SolverEngine() {
    	setSolver(SolverType.BDD);
    }

    public static SolverEngine getEngine() {
        return solverEngine;
    }
    
    public static SolverLib getSolver() {
        return solverImpl;
    }
    
    public static void resetEngine() {
    	resetEngine(solverType);
    }

    public static void resetEngine(SolverType type) {
    	setSolver(type);
    }

    public static void cleanupEngine() {
        solverImpl.cleanup();
    }
    
    public static void setSolver(SolverType type) {
    	solverType = type;
    	switch(type) {
    	case BDD:		solverImpl = new PJBDDImpl(false);
    		break;
    	case CBDD:		solverImpl = new PJBDDImpl(true);
    		break;
        default:        solverImpl = new SatGuard(type);
            Aig.resetAig();
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
