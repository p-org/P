package psymbolic.valuesummary.solvers;

public interface SolverLib<T> {
    T constFalse();

    T constTrue();

    boolean isFalse(T formula);

    boolean isTrue(T formula);

    T and(T left, T right);

    T or(T left, T right);

    T not(T bdd);

    T implies(T left, T right);

    T ifThenElse(T cond, T thenClause, T elseClause);

    T newVar();

    String toString(T bdd);

    T fromString(String s);
    
    int getVarCount();

    int getNodeCount();

    String getStats();

    void cleanup();
    
}
