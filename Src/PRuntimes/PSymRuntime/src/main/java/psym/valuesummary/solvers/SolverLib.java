package psym.valuesummary.solvers;

public interface SolverLib<T> {
    T constFalse();

    T constTrue();

    boolean isSat(T formula);

    T and(T left, T right);

    T or(T left, T right);

    T not(T bdd);

    T newVar(String name);

    T simplify(T formula);

    String toString(T bdd);

    T fromString(String s);

    int getVarCount();

    int getNodeCount();

    int getExprCount();

    String getStats();

    void cleanup();

    boolean areEqual(T left, T right);

    int hashCode(T formula);

}
