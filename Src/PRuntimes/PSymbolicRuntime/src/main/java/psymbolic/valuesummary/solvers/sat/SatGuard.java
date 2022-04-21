package psymbolic.valuesummary.solvers.sat;

import lombok.Getter;
import psymbolic.valuesummary.solvers.SolverLib;
import psymbolic.valuesummary.solvers.SolverType;
import psymbolic.valuesummary.solvers.sat.expr.ExprLibType;

public class SatGuard implements SolverLib<SatExpr> {
    private static SatLib satImpl;

    @Getter
    private static SolverType solverType;

    public SatGuard(SolverType st, ExprLibType et) {
        SatExpr.setExprLib(et);
        setSolver(st);
    }

    public static SatLib getSolver() {
        return satImpl;
    }

    public static void setSolver(SolverType type) {
        solverType = type;
        switch(type) {
            case ABC:	                satImpl = new ABCImpl();
                break;
            case CVC5:	                satImpl = new CVC5Impl();
                break;
            case YICES2:	            satImpl = new YicesImpl();
                break;
            case Z3:	                satImpl = new Z3Impl();
                break;
            case JAVASMT_BOOLECTOR:
            case JAVASMT_CVC4:
            case JAVASMT_MATHSAT5:
            case JAVASMT_PRINCESS:
            case JAVASMT_SMTINTERPOL:
            case JAVASMT_YICES2:
            case JAVASMT_Z3:            satImpl = new JavaSmtImpl(type);
                break;
            default:
                throw new RuntimeException("Unexpected solver configuration of type " + type);
        }
    }

    public boolean isSat(SatExpr formula) {
        return SatExpr.isSat(formula);
    }

    public SatExpr constFalse() {
        return SatExpr.ConstFalse();
    }

    public SatExpr constTrue() {
        return SatExpr.ConstTrue();
    }

    public SatExpr newVar(String name) {
        return SatExpr.NewVar(name);
    }

    public SatExpr and(SatExpr left, SatExpr right) {
        return SatExpr.And(left, right);
    }

    public SatExpr or(SatExpr left, SatExpr right) {
        return SatExpr.Or(left, right);
    }

    public SatExpr not(SatExpr formula) {
        return SatExpr.Not(formula);
    }

    public SatExpr implies(SatExpr left, SatExpr right) {
        return SatExpr.Or(SatExpr.Not(left), right);
    }

    public SatExpr ifThenElse(SatExpr cond, SatExpr thenClause, SatExpr elseClause) {
        return SatExpr.Or(SatExpr.And(cond, thenClause),
                          SatExpr.And(SatExpr.Not(cond), elseClause));
    }

    public SatExpr simplify(SatExpr formula) {
        return SatExpr.Simplify(formula);
    }

    public String toString(SatExpr formula) {
        return formula.toString();
    }

    public SatExpr fromString(String s) {
        if (s.equals("false")) {
            return constFalse();
        }
        if (s.equals("true")) {
            return constTrue();
        }
        throw new RuntimeException("Unsupported");
    }

    public int getVarCount() {
        return SatExpr.numVars;
    }

    public int getNodeCount() {
        return getSolver().getNodeCount();
    }

    public int getExprCount() {
        return SatExpr.getExprImpl().getExprCount();
    }

    public String getStats() {
        return SatExpr.getExprImpl().getStats() + "\n" + satImpl.getStats();
    }

    public void cleanup() {
        satImpl.cleanup();
    }

    public boolean areEqual(SatExpr left, SatExpr right) {
        return left.equals(right);
    }

    public int hashCode(SatExpr formula) {
        return formula.hashCode();
    }

}
