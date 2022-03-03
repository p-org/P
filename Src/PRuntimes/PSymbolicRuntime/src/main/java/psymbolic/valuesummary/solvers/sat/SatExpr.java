package psymbolic.valuesummary.solvers.sat;

import com.bpodgursky.jbool_expressions.Expression;
import com.bpodgursky.jbool_expressions.Literal;
import com.bpodgursky.jbool_expressions.Variable;
import com.bpodgursky.jbool_expressions.And;
import com.bpodgursky.jbool_expressions.Or;
import com.bpodgursky.jbool_expressions.Not;
import com.bpodgursky.jbool_expressions.options.ExprOptions;
import com.bpodgursky.jbool_expressions.rules.RuleSet;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;

public class SatExpr {
    public static int numVars = 0;
    private static Expression<String> exprTrue = Literal.getTrue();
    private static Expression<String> exprFalse = Literal.getFalse();
    public static HashMap<Expression<String>, SatObject> table = new HashMap<Expression<String>, SatObject>();

    private Expression<String> expr;

    public SatExpr(Expression<String> expr) {
        this.expr = expr;
    }

    private static SatObject createSatFormula(Expression<String> expr) {
        if (table.containsKey(expr)) {
            return table.get(expr);
        }
//        System.out.println("Creating Sat formula for " + toString(expr));

        SatObject satFormula = new SatObject(null, SatStatus.Unknown);
        String exprType = expr.getExprType();

        List<Object> satChildren = new ArrayList<>();
        for (Expression<String> child: expr.getChildren()) {
//            Expression<String> child = RuleSet.simplify(child, ExprOptions.allCacheIntern());
            SatObject satChild = createSatFormula(child);
            satChildren.add(satChild.formula);
        }

        switch(exprType) {
            case "variable":
                satFormula.formula = SatGuard.getSolver().newVar(expr.toString());
                break;
            case "literal":
                if (expr == exprTrue) {
                    satFormula.formula = SatGuard.getSolver().constTrue();
                    satFormula.status = SatStatus.Sat;
                } else {
                    assert(expr == exprFalse);
                    satFormula.formula = SatGuard.getSolver().constFalse();
                    satFormula.status = SatStatus.Unsat;
                }
                break;
            case "not":
                assert (satChildren.size() == 1);
                satFormula.formula = SatGuard.getSolver().not(satChildren.get(0));
                satFormula.status = SatStatus.Unknown;
                break;
            case "and":
                satFormula.formula = SatGuard.getSolver().and(satChildren);
                satFormula.status = SatStatus.Unknown;
                break;
            case "or":
                satFormula.formula = SatGuard.getSolver().or(satChildren);
                satFormula.status = SatStatus.Unknown;
                break;
            default:
                throw new RuntimeException("Unexpected expr of type " + exprType + " : " + toString(expr));
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

    private static SatExpr newExpr(Expression<String> original) {
        Expression<String> simplified = RuleSet.simplify(original, ExprOptions.onlyCaching());
//        System.out.println("\toriginal term  : " + toString(original));
//        System.out.println("\tsimplified term: " + toString(simplified));
        return new SatExpr(simplified);
//        return new SatExpr(original);
    }

    public static SatExpr ConstFalse() {
        return newExpr(exprFalse);
    }

    public static SatExpr ConstTrue() {
        return newExpr(exprTrue);
    }

    public static SatExpr And(SatExpr left, SatExpr right) {
        return newExpr(And.of(left.expr, right.expr));
    }

    public static SatExpr Or(SatExpr left, SatExpr right) {
        return newExpr(Or.of(left.expr, right.expr));
//        return newExpr(Not.of(And.of(Not.of(left.expr), Not.of(right.expr))));
    }

    public static SatExpr Not(SatExpr formula) {
        return newExpr(Not.of(formula.expr));
    }

    public static SatExpr NewVar() {
        return newExpr(Variable.of("x" + numVars++));
    }

    private static String toString(Expression<String> formula) {
        return String.format("%.80s", formula);
    }

    @Override
    public String toString() {
        return toString(this.expr);
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
