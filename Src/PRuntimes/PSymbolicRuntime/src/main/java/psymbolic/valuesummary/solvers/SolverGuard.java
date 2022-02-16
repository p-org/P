package psymbolic.valuesummary.solvers;

import psymbolic.runtime.statistics.SolverStats;
import java.util.List;

/**
    Represents the generic solver based implementation of Guard
 */
public class SolverGuard {
    private final Object formula;

    public SolverGuard(Object formula) {
        this.formula = formula;
    }

    public static SolverGuard constFalse() {
        return new SolverGuard(SolverEngine.getSolver().constFalse());
    }

    public static SolverGuard constTrue() {
        return new SolverGuard(SolverEngine.getSolver().constTrue());
    }

    public boolean isFalse() {
    	SolverStats.isFalseOperations++;
        if (SolverEngine.getSolver().isFalse(formula)) {
        	SolverStats.isFalseResult++;
        	return true;
        } else {
        	return false;
        }
    }

    public boolean isTrue() {
    	SolverStats.isTrueOperations++;
        if (SolverEngine.getSolver().isTrue(formula)) {
        	SolverStats.isTrueResult++;
        	return true;
        } else {
        	return false;
        }
    }

    public SolverGuard and(SolverGuard other) {
    	SolverStats.andOperations++;
        return new SolverGuard(SolverEngine.getSolver().and(formula, other.formula));
    }

    public SolverGuard or(SolverGuard other) {
    	SolverStats.orOperations++;
        return new SolverGuard(SolverEngine.getSolver().or(formula, other.formula));
    }

    public SolverGuard implies(SolverGuard other) { 
    	return new SolverGuard(SolverEngine.getSolver().implies(formula, other.formula)); 
    }

    public SolverGuard not() {
    	SolverStats.notOperations++;
        return new SolverGuard(SolverEngine.getSolver().not(formula));
    }

    public static SolverGuard orMany(List<SolverGuard> others) {
        return others.stream().reduce(SolverGuard.constFalse(), SolverGuard::or);
    }

    public SolverGuard ifThenElse(SolverGuard thenCase, SolverGuard elseCase) {
        return new SolverGuard(SolverEngine.getSolver().ifThenElse(formula, thenCase.formula, elseCase.formula));
    }

    public static SolverGuard newVar() {
        return new SolverGuard(SolverEngine.getSolver().newVar());
    }

    @Override
    public String toString() {
        return SolverEngine.getSolver().toString(formula);
    }
    
}
