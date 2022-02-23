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

    private static SolverGuard getSolverGuard(Object formula) {
        return new SolverGuard(formula);
    }

    public static SolverGuard constFalse() {
        return getSolverGuard(SolverEngine.getSolver().constFalse());
    }

    public static SolverGuard constTrue() {
        return getSolverGuard(SolverEngine.getSolver().constTrue());
    }

    public boolean isTrue() {
        return SolverEngine.getSolver().isSat(SolverEngine.getSolver().not(formula));
    }

    public boolean isFalse() {
        return !SolverEngine.getSolver().isSat(formula);
    }

    public SolverGuard and(SolverGuard other) {
    	SolverStats.andOperations++;
        return getSolverGuard(SolverEngine.getSolver().and(formula, other.formula));
    }

    public SolverGuard or(SolverGuard other) {
    	SolverStats.orOperations++;
        return getSolverGuard(SolverEngine.getSolver().or(formula, other.formula));
    }

    public SolverGuard implies(SolverGuard other) { 
    	return getSolverGuard(SolverEngine.getSolver().implies(formula, other.formula));
    }

    public SolverGuard not() {
    	SolverStats.notOperations++;
        return getSolverGuard(SolverEngine.getSolver().not(formula));
    }

    public static SolverGuard orMany(List<SolverGuard> others) {
        return others.stream().reduce(SolverGuard.constFalse(), SolverGuard::or);
    }

    public SolverGuard ifThenElse(SolverGuard thenCase, SolverGuard elseCase) {
        return getSolverGuard(SolverEngine.getSolver().ifThenElse(formula, thenCase.formula, elseCase.formula));
    }

    public static SolverGuard newVar() {
        return getSolverGuard(SolverEngine.getSolver().newVar());
    }

    @Override
    public String toString() {
        return SolverEngine.getSolver().toString(formula);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (!(o instanceof SolverGuard)) return false;
        SolverGuard that = (SolverGuard) o;
        return formula.equals(that.formula);
    }
}
