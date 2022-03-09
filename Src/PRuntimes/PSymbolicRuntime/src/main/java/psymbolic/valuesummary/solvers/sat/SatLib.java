package psymbolic.valuesummary.solvers.sat;

import java.util.List;

public interface SatLib<T> {
    T constFalse();

    T constTrue();

    boolean isSat(T formula);

    T and(List<T> children);

    T or(List<T> children);

    T not(T formula);

    T newVar(String name);

    String toString(T formula);

    T fromString(String s);

    int getNodeCount();

    String getStats();

    void cleanup();

    boolean areEqual(T left, T right);

}
