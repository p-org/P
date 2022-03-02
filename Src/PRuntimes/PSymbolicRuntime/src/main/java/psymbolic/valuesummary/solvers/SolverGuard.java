package psymbolic.valuesummary.solvers;

import psymbolic.runtime.statistics.SolverStats;

import java.util.HashMap;
import java.util.List;

/**
    Represents the generic solver based implementation of Guard
 */
public class SolverGuard {
    private final Object formula;
    private SolverTrueStatus statusTrue;
    private SolverFalseStatus statusFalse;
    private static HashMap<Object, SolverGuard> table = new HashMap<Object, SolverGuard>();

    public SolverGuard(Object formula) {
        this.formula = formula;
        this.statusTrue = SolverTrueStatus.Unknown;
        this.statusFalse = SolverFalseStatus.Unknown;
    }

    public boolean isTrue() {
        switch (statusTrue) {
            case True:
                return true;
            case NotTrue:
                return false;
            default:
                boolean isSatNeg = SolverEngine.getSolver().isSat(SolverEngine.getSolver().not(formula));
                if (!isSatNeg) {
                    statusTrue = SolverTrueStatus.True;
                    return true;
                } else {
                    statusTrue = SolverTrueStatus.NotTrue;
                    return false;
                }
        }
    }

    public boolean isFalse() {
        switch (statusFalse) {
            case False:
                return true;
            case NotFalse:
                return false;
            default:
                boolean isSat = SolverEngine.getSolver().isSat(formula);
                if (!isSat) {
                    statusFalse = SolverFalseStatus.False;
                    return true;
                } else {
                    statusFalse = SolverFalseStatus.NotFalse;
                    return false;
                }
        }
    }

    private static SolverGuard getSolverGuard(Object formula) {
        if (table.containsKey(formula)) {
            return table.get(formula);
        }
        SolverGuard newGuard = new SolverGuard(formula);
        table.put(formula, newGuard);
//        System.out.println("Creating new SolverGuard: " + newGuard.toString());
        return newGuard;
    }

    public static int getGuardCount() {
        return table.size();
    }

    public static SolverGuard constFalse() {
        SolverGuard g = getSolverGuard(SolverEngine.getSolver().constFalse());
        g.statusTrue = SolverTrueStatus.NotTrue;
        g.statusFalse = SolverFalseStatus.False;
        return g;
    }

    public static SolverGuard constTrue() {
        SolverGuard g = getSolverGuard(SolverEngine.getSolver().constTrue());
        g.statusTrue = SolverTrueStatus.True;
        g.statusFalse = SolverFalseStatus.NotFalse;
        return g;
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
        return SolverEngine.getSolver().areEqual(formula, that.formula);
//        return SolverEngine.getSolver().areEqual(formula, that.formula) && statusTrue.equals(that.statusTrue) && statusFalse.equals(that.statusFalse);
    }
}
