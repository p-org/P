package psymbolic.valuesummary.solvers;

import psymbolic.runtime.statistics.SolverStats;
import psymbolic.valuesummary.solvers.bdd.PJBDDImpl;
import psymbolic.valuesummary.solvers.sat.JavaSmtImpl;
import java.util.List;

/**
    Represents the generic solver based implementation of Guard
 */
public class SolverGuard {

    private static SolverLib solverImpl = new JavaSmtImpl();

    public static SolverLib getInstance() {
        return solverImpl;
    }

    public static void resetSolver() {
        solverImpl = new JavaSmtImpl();
    }

    public static void cleanup() {
        solverImpl.UnusedNodeCleanUp();
    }

    private final Object formula;

    public SolverGuard(Object formula) {
        this.formula = formula;
    }

    public static SolverGuard constFalse() {
        return new SolverGuard(solverImpl.constFalse());
    }

    public static SolverGuard constTrue() {
        return new SolverGuard(solverImpl.constTrue());
    }

    public boolean isFalse() {
    	SolverStats.isFalseOperations++;
        if (solverImpl.isFalse(formula)) {
        	SolverStats.isFalseResult++;
        	return true;
        } else {
        	return false;
        }
    }

    public boolean isTrue() {
    	SolverStats.isTrueOperations++;
        if (solverImpl.isTrue(formula)) {
        	SolverStats.isTrueResult++;
        	return true;
        } else {
        	return false;
        }
    }

    public SolverGuard and(SolverGuard other) {
    	SolverStats.andOperations++;
        return new SolverGuard(solverImpl.and(formula, other.formula));
    }

    public SolverGuard or(SolverGuard other) {
    	SolverStats.orOperations++;
        return new SolverGuard(solverImpl.or(formula, other.formula));
    }

    public SolverGuard implies(SolverGuard other) { 
    	return new SolverGuard(solverImpl.implies(formula, other.formula)); 
    }

    public SolverGuard not() {
    	SolverStats.notOperations++;
        return new SolverGuard(solverImpl.not(formula));
    }

    public static SolverGuard orMany(List<SolverGuard> wrappedBdd) {
        return wrappedBdd.stream().reduce(SolverGuard.constFalse(), SolverGuard::or);
    }

    public SolverGuard ifThenElse(SolverGuard thenCase, SolverGuard elseCase) {
        return new SolverGuard(solverImpl.ifThenElse(formula, thenCase.formula, elseCase.formula));
    }

    public static SolverGuard newVar() {
        return new SolverGuard(solverImpl.newVar());
    }

    @Override
    public String toString() {
        return solverImpl.toString(formula);
    }
    
}
