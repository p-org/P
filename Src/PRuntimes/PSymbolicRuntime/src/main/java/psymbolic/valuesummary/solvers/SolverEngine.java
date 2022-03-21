package psymbolic.valuesummary.solvers;

import lombok.Getter;
import lombok.Setter;
import psymbolic.valuesummary.solvers.bdd.PJBDDImpl;
import psymbolic.valuesummary.solvers.sat.expr.ExprLibType;
import psymbolic.valuesummary.solvers.sat.SatGuard;

/**
 * Represents the generic backend engine
 */
public class SolverEngine {
    @Getter @Setter
    private static SolverLib solver;
    @Getter @Setter
    private static SolverType solverType;
    @Getter @Setter
    private static ExprLibType exprLibType;

    public static void switchEngineAuto() {
        switch (getSolverType()) {
            case BDD:
            case CBDD:
                if (SolverEngine.getSolver().getNodeCount() > 20000000) {
                    switchEngine(SolverType.YICES2, ExprLibType.Fraig);
                }
                break;
        }
    }

    public static void switchEngine(SolverType type, ExprLibType etype) {
        if (type == getSolverType() && etype == getExprLibType())
            return;
        System.out.println("Switching solver engine:\n\t"
                            + getSolverType().toString() + "\t-> " + type.toString() + "\n\t"
                            + getExprLibType().toString() + "\t-> " + etype.toString());
        setSolver(type, etype);
        SolverGuard.switchSolverGuard();
    }

    public static void resetEngine(SolverType type, ExprLibType etype) {
    	setSolver(type, etype);
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
