package psymbolic.valuesummary.solvers;

import com.google.common.collect.ImmutableList;
import psymbolic.runtime.statistics.SolverStats;
import psymbolic.valuesummary.solvers.sat.SatExpr;

import java.util.*;

import static java.util.Objects.hash;

/**
    Represents the generic solver based implementation of Guard
 */
public class SolverGuard {
    private Object formula;
    private final SolverGuardType type;
    private final ImmutableList<SolverGuard> children;

    private SolverTrueStatus statusTrue;
    private SolverFalseStatus statusFalse;
//    private static SolverGuard exprTrue = null;
//    private static SolverGuard exprFalse = null;
    private static List<SolverGuard> varList = new ArrayList<>();
    private static HashMap<Object, SolverGuard> table = new HashMap<Object, SolverGuard>();

    public SolverGuard(Object formula, SolverGuardType type, ImmutableList<SolverGuard> children) {
        this.formula = formula;
        this.type = type;
        this.children = children;
        this.statusTrue = SolverTrueStatus.Unknown;
        this.statusFalse = SolverFalseStatus.Unknown;
    }

    public static void reset() {
        table.clear();
        varList.clear();
//        exprTrue = createTrue();
//        exprFalse = createFalse();
    }

    public static void simplifySolverGuard() {
        // create a local copy to iterate over old guards
        HashMap<Object, SolverGuard> oldTable = new HashMap<Object, SolverGuard>(table);

        // reset the old table
        table.clear();

        // recreate all vars first (in order)
        for (SolverGuard oldGuard : varList) {
            simplifySolverGuard(oldGuard);
        }

        // recreate remaining guards
        for (SolverGuard oldGuard : oldTable.values()) {
            simplifySolverGuard(oldGuard);
        }
    }

    private static void simplifySolverGuard(SolverGuard original) {
        // return if already cached in new table
        if (table.containsKey(original.formula)) {
            original.formula = table.get(original.formula).formula;
            return;
        }

//        // process children first
//        for (SolverGuard child: original.children) {
//            simplifySolverGuard(child);
//        }

        original.formula = SolverEngine.getSolver().simplify(original.formula);

        // cache result
        table.put(original.formula, original);
    }

    public static void switchSolverGuard() {
        // create a local copy to iterate over old guards
        HashMap<Object, SolverGuard> oldTable = new HashMap<Object, SolverGuard>(table);

        // reset the old table
        table.clear();

        // recreate all vars first (in order)
        for (SolverGuard oldGuard : varList) {
            recreateSolverGuard(oldGuard);
        }

        // recreate remaining guards
        for (SolverGuard oldGuard : oldTable.values()) {
            recreateSolverGuard(oldGuard);
        }
    }

    private static void recreateSolverGuard(SolverGuard original) {
        // return if already cached in new table
        if (table.containsKey(original.formula)) {
            original.formula = table.get(original.formula).formula;
            return;
        }

        // process children first
        for (SolverGuard child: original.children) {
            recreateSolverGuard(child);
        }

        // recreate new solver object
        switch(original.type) {
            case TRUE:
                original.formula = SolverEngine.getSolver().constTrue();
                break;
            case FALSE:
                original.formula = SolverEngine.getSolver().constFalse();
                break;
            case VARIABLE:
                original.formula = SolverEngine.getSolver().newVar();
                break;
            case NOT:
                assert (original.children.size() == 1);
                original.formula = SolverEngine.getSolver().not(original.children.get(0).formula);
                break;
            case AND:
                assert (original.children.size() == 2);
                original.formula = SolverEngine.getSolver().and(original.children.get(0).formula,
                                                                original.children.get(1).formula);
                break;
            case OR:
                assert (original.children.size() == 2);
                original.formula = SolverEngine.getSolver().or( original.children.get(0).formula,
                                                                original.children.get(1).formula);
                break;
            default:
                throw new RuntimeException("Unexpected solver guard of type " + original.type + " : " + original);
        }

        // cache result
        table.put(original.formula, original);
    }

    public boolean isTrue() {
        switch (statusTrue) {
            case True:
                return true;
            case NotTrue:
                return false;
            default:
                checkInput(Arrays.asList(this));
                boolean isSatNeg = SolverEngine.getSolver().isSat(SolverEngine.getSolver().not(formula));
                if (!isSatNeg) {
                    statusTrue = SolverTrueStatus.True;
                    statusFalse = SolverFalseStatus.NotFalse;
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
                checkInput(Arrays.asList(this));
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
        return newGuard;
    }

    public static int getGuardCount() {
        return table.size();
    }

    private static SolverGuard createTrue() {
        SolverGuard g = getSolverGuard( SolverEngine.getSolver().constTrue(),
                                        SolverGuardType.TRUE,
                                        ImmutableList.of());
        g.statusTrue = SolverTrueStatus.True;
        g.statusFalse = SolverFalseStatus.NotFalse;
        return g;
    }

    private static SolverGuard createFalse() {
        SolverGuard g = getSolverGuard( SolverEngine.getSolver().constFalse(),
                SolverGuardType.FALSE,
                ImmutableList.of());
        g.statusTrue = SolverTrueStatus.NotTrue;
        g.statusFalse = SolverFalseStatus.False;
        return g;
    }

    public static SolverGuard constTrue() {
        return createTrue();
    }

    public static SolverGuard constFalse() {
        return createFalse();
    }

    public static SolverGuard newVar() {
        SolverGuard g = getSolverGuard( SolverEngine.getSolver().newVar(),
                                        SolverGuardType.VARIABLE,
                                        ImmutableList.of());
        g.statusTrue = SolverTrueStatus.NotTrue;
        g.statusFalse = SolverFalseStatus.NotFalse;
        varList.add(g);
        return g;
    }

    private static void checkInput(List<SolverGuard> inputs) {
        for (SolverGuard input : inputs) {
            if (!table.containsKey(input.formula)) {
                System.out.println("\tMissing SolverGuard: " + SolverEngine.getSolver().toString(input.formula));
                System.out.println("\thashcode: " + SolverEngine.getSolver().hashCode(input.formula));
                assert(false);
            }
        }
    }

    public SolverGuard not() {
        checkInput(Arrays.asList(this));
        SolverStats.notOperations++;
        return getSolverGuard(  SolverEngine.getSolver().not(formula),
                SolverGuardType.NOT,
                ImmutableList.of(this));
    }

    public SolverGuard and(SolverGuard other) {
        checkInput(Arrays.asList(this, other));
    	SolverStats.andOperations++;
        return getSolverGuard(  SolverEngine.getSolver().and(formula, other.formula),
                                SolverGuardType.AND,
                                ImmutableList.of(this, other));
    }

    public SolverGuard or(SolverGuard other) {
        checkInput(Arrays.asList(this, other));
    	SolverStats.orOperations++;
        return getSolverGuard(SolverEngine.getSolver().or(  formula, other.formula),
                                                            SolverGuardType.OR,
                                                            ImmutableList.of(this, other));
    }

    private static SolverGuard orMany(List<SolverGuard> others) {
        return others.stream().reduce(SolverGuard.constFalse(), SolverGuard::or);
    }

    public SolverGuard implies(SolverGuard other) {
        checkInput(Arrays.asList(this, other));
        return (this.not()).or(other);
    }

    public SolverGuard ifThenElse(SolverGuard thenCase, SolverGuard elseCase) {
        checkInput(Arrays.asList(this, thenCase, elseCase));
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
