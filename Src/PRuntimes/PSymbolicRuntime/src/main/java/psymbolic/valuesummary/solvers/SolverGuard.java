package psymbolic.valuesummary.solvers;

import com.google.common.collect.ImmutableList;
import psymbolic.runtime.statistics.SolverStats;

import java.util.Arrays;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;

/**
    Represents the generic solver based implementation of Guard
 */
public class SolverGuard {
    private final Object formula;
    private final SolverGuardType type;
    private final ImmutableList<SolverGuard> children;

    private SolverTrueStatus statusTrue;
    private SolverFalseStatus statusFalse;
    private static HashMap<Object, SolverGuard> table = new HashMap<Object, SolverGuard>();

    public SolverGuard(Object formula, SolverGuardType type, ImmutableList<SolverGuard> children) {
        this.formula = formula;
        this.type = type;
        this.children = children;
        this.statusTrue = SolverTrueStatus.Unknown;
        this.statusFalse = SolverFalseStatus.Unknown;
    }

    public static void switchSolverGuard(SolverLib oldSolver) {
        // TODO
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
                    statusFalse = SolverFalseStatus.False;
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
                    statusTrue = SolverTrueStatus.NotTrue;
                    statusFalse = SolverFalseStatus.False;
                    return true;
                } else {
                    statusFalse = SolverFalseStatus.NotFalse;
                    return false;
                }
        }
    }

    private static SolverGuard getSolverGuard(Object formula, SolverGuardType type, ImmutableList<SolverGuard> children) {
        if (table.containsKey(formula)) {
            return table.get(formula);
        }
        SolverGuard newGuard = new SolverGuard(formula, type, children);
        table.put(formula, newGuard);
//        System.out.println("Creating new SolverGuard: " + newGuard.toString());
        return newGuard;
    }

    public static int getGuardCount() {
        return table.size();
    }

    public static SolverGuard constFalse() {
        SolverGuard g = getSolverGuard( SolverEngine.getSolver().constFalse(),
                                        SolverGuardType.FALSE,
                                        ImmutableList.of());
        g.statusTrue = SolverTrueStatus.NotTrue;
        g.statusFalse = SolverFalseStatus.False;
        return g;
    }

    public static SolverGuard constTrue() {
        SolverGuard g = getSolverGuard( SolverEngine.getSolver().constTrue(),
                                        SolverGuardType.TRUE,
                                        ImmutableList.of());
        g.statusTrue = SolverTrueStatus.True;
        g.statusFalse = SolverFalseStatus.NotFalse;
        return g;
    }

    public static SolverGuard newVar() {
        SolverGuard g = getSolverGuard( SolverEngine.getSolver().newVar(),
                                        SolverGuardType.VARIABLE,
                                        ImmutableList.of());
        g.statusTrue = SolverTrueStatus.NotTrue;
        g.statusFalse = SolverFalseStatus.NotFalse;
        return g;
    }

    public SolverGuard and(SolverGuard other) {
    	SolverStats.andOperations++;
        return getSolverGuard(  SolverEngine.getSolver().and(formula, other.formula),
                                SolverGuardType.AND,
                                ImmutableList.of(this, other));
    }

    public SolverGuard or(SolverGuard other) {
    	SolverStats.orOperations++;
        return getSolverGuard(SolverEngine.getSolver().or(  formula, other.formula),
                                                            SolverGuardType.OR,
                                                            ImmutableList.of(this, other));
    }

    public SolverGuard not() {
    	SolverStats.notOperations++;
        return getSolverGuard(  SolverEngine.getSolver().not(formula),
                                SolverGuardType.NOT,
                                ImmutableList.of(this));
    }

    public static SolverGuard orMany(List<SolverGuard> others) {
        return others.stream().reduce(SolverGuard.constFalse(), SolverGuard::or);
    }

    public SolverGuard implies(SolverGuard other) {
        return (this.not()).or(other);
    }

    public SolverGuard ifThenElse(SolverGuard thenCase, SolverGuard elseCase) {
        return (this.and(thenCase)).or((this.not()).and(elseCase));
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

    @Override
    public int hashCode() {
        return SolverEngine.getSolver().hashCode(formula);
    }

}
