package psymbolic.valuesummary.solvers.sat;

import psymbolic.runtime.statistics.SolverStats;

import java.util.List;
import psymbolic.valuesummary.solvers.sat.expr.Fraig;
import psymbolic.valuesummary.solvers.sat.expr.ExprLibType;

public class ABCImpl implements SatLib<Long> {
    private Fraig exprImpl;

    public ABCImpl() {
        assert(SatExpr.getExprType() == ExprLibType.Fraig);
        exprImpl = (Fraig) SatExpr.getExprImpl();
    }

    public Long constFalse() {
        return exprImpl.getFalse();
    }

    public Long constTrue() {
        return exprImpl.getTrue();
    }

    public boolean isSat(Long formula) {
        SolverStats.isSatOperations++;
        switch(exprImpl.isSat(formula, -1)) {
            case Sat:
                SolverStats.isSatResult++;
                return true;
            case Unsat:
                return false;
            default:
                throw new RuntimeException("ABC returned query result unknown for " + toString(formula));
        }
    }

    public Long and(List<Long> children) {
        throw new RuntimeException("Unsupported");
    }

    public Long or(List<Long> children) {
        throw new RuntimeException("Unsupported");
    }

    public Long not(Long formula) {
        throw new RuntimeException("Unsupported");
    }

    public Long newVar(String name) {
        throw new RuntimeException("Unsupported");
    }

    public String toString(Long formula) {
        return exprImpl.toString(formula);
    }

    public Long fromString(String s) {
        throw new RuntimeException("Unsupported");
    }

    public int getNodeCount() {
        return exprImpl.idSet.size();
    }

    public String getStats() {
        return exprImpl.getStats();
    }

    public void cleanup() {
        // TODO
    }

    public boolean areEqual(Long left, Long right) {
        return exprImpl.areEqual(left, right);
    }

}
