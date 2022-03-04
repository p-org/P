package psymbolic.valuesummary.solvers.sat;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Arrays;

public class SatExpr {
    public static int numVars = 0;
    public static HashMap<Expr, SatObject> table = new HashMap<Expr, SatObject>();
    private Expr expr;

    public SatExpr(Expr expr) {
        this.expr = expr;
    }

    private static SatObject createSatFormula(Expr original) {
        if (table.containsKey(original)) {
            return table.get(original);
        }
        Expr expr = original;
//        Expr expr = Expr.simplify(original);
//        System.out.println("Creating Sat formula for " + toString(expr));

        SatObject satFormula = new SatObject(null, SatStatus.Unknown);
        ExprType exprType = expr.getType();

        List<Object> satChildren = new ArrayList<>();
        for (Expr child: expr.getChildren()) {
            SatObject satChild = createSatFormula(child);
            satChildren.add(satChild.formula);
        }

        switch(exprType) {
            case TRUE:
                satFormula.formula = SatGuard.getSolver().constTrue();
                satFormula.status = SatStatus.Sat;
                break;
            case FALSE:
                satFormula.formula = SatGuard.getSolver().constFalse();
                satFormula.status = SatStatus.Unsat;
                break;
            case VARIABLE:
                satFormula.formula = SatGuard.getSolver().newVar(expr.toString());
                break;
            case NOT:
                assert (satChildren.size() == 1);
                satFormula.formula = SatGuard.getSolver().not(satChildren.get(0));
                satFormula.status = SatStatus.Unknown;
                break;
            case AND:
                satFormula.formula = SatGuard.getSolver().and(satChildren);
                satFormula.status = SatStatus.Unknown;
                break;
            case OR:
                satFormula.formula = SatGuard.getSolver().or(satChildren);
                satFormula.status = SatStatus.Unknown;
                break;
            default:
                throw new RuntimeException("Unexpected expr of type " + exprType + " : " + expr);
        }
        table.put(expr, satFormula);
        return satFormula;
    }

    private static boolean checkSat(SatObject satFormula) {
        switch (satFormula.status) {
            case Sat:
                return true;
            case Unsat:
                return false;
            default:
//            	System.out.println("Checking formula: " + SatGuard.getSolver().toString(satFormula.formula));
                boolean isSat = SatGuard.getSolver().isSat(satFormula.formula);
//            	System.out.println("Result: " + isSat);
                if (isSat) {
                    satFormula.status = SatStatus.Sat;
                    return true;
                } else {
                    satFormula.status = SatStatus.Unsat;
                    // Also update the sat formula node to FALSE
                    satFormula.formula = SatGuard.getSolver().constFalse();
                    return false;
                }
        }
    }

    public static boolean isSat(SatExpr formula) {
        SatObject satFormula = createSatFormula(formula.expr);
        return checkSat(satFormula);
    }

    private static SatExpr newExpr(Expr original) {
        return new SatExpr(original);
    }

    public static SatExpr ConstTrue() {
        return newExpr(Expr.getTrue());
    }

    public static SatExpr ConstFalse() {
        return newExpr(Expr.getFalse());
    }

    public static SatExpr NewVar() {
        return newExpr(Expr.newVar("x" + numVars++));
    }

    public static SatExpr Not(SatExpr formula) {
        return newExpr(Expr.not(formula.expr));
    }

    public static SatExpr And(SatExpr left, SatExpr right) {
        return newExpr(Expr.and(Arrays.asList(left.expr, right.expr)));
    }

    public static SatExpr Or(SatExpr left, SatExpr right) {
        return newExpr(Expr.or(Arrays.asList(left.expr, right.expr)));
    }

    @Override
    public String toString() {
        return this.expr.toString();
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (!(o instanceof SatExpr)) return false;
        SatExpr that = (SatExpr) o;
        return this.expr.equals(that.expr);
    }

    @Override
    public int hashCode() {
        return this.expr.hashCode();
    }
}
