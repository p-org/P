package psymbolic.valuesummary.solvers.sat.expr;

import psymbolic.valuesummary.solvers.SolverGuardType;

import java.util.List;

public interface ExprLib<T> {
    void reset();

    T getTrue();

    T getFalse();

    T newVar(String name);

    T not(T child);

    T and(T childA, T childB);

    T or(T childA, T childB);

    T simplify(T child);

    SolverGuardType getType(T formula);

    List<T> getChildren(T formula);

    String toString(T formula);

    String getStats();

    int getExprCount();

    boolean areEqual(T left, T right);

    int getHashCode(T formula);

}
