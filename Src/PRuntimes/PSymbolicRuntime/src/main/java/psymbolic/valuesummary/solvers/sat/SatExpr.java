package psymbolic.valuesummary.solvers.sat;

import com.berkeley.abc.Abc;
import lombok.Getter;
import psymbolic.runtime.logger.SearchLogger;
import psymbolic.valuesummary.solvers.SolverGuardType;
import psymbolic.valuesummary.solvers.SolverType;
import psymbolic.valuesummary.solvers.sat.expr.*;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;

public class SatExpr {
    private static boolean abcStarted = false;
    public static int numVars = 0;
    public static HashMap<Object, SatObject> satTable = new HashMap<Object, SatObject>();
    public static HashMap<Object, SatObject> aigTable = new HashMap<Object, SatObject>();
    private static ExprLib exprImpl;
    @Getter
    private static ExprLibType exprType;

    private Object expr;

    public static void resetAbc() {
        if (abcStarted) {
            System.out.println("Stopping ABC");
            Abc.Abc_Stop();
        }
        abcStarted = true;
        System.out.println("Starting ABC");
        Abc.Abc_Start();
    }

    public static ExprLib getExprImpl() {
        return exprImpl;
    }

    public static void setExprLib(ExprLibType type) {
        exprType = type;
        resetAbc();
        switch(type) {
            case Aig:	            exprImpl = new Aig();
                break;
            case Fraig:	            exprImpl = new Fraig();
                break;
            case Iaig:	            exprImpl = new Iaig();
                break;
            case NativeExpr:	    exprImpl = new NativeExpr();
                break;
            default:
                throw new RuntimeException("Unexpected/incompatible expression library type " + type);
        }
    }

    public SatExpr(Object expr) {
        this.expr = expr;
    }

    private static SatObject createSatFormula(Object original) {
        if (satTable.containsKey(original)) {
            return satTable.get(original);
        } else if (SatGuard.getSolverType() == SolverType.ABC) {
            if (aigTable.containsKey(original)) {
                return aigTable.get(original);
            }
            SatObject satFormula = new SatObject(original, SatStatus.Unknown);
            satTable.put(original, satFormula);
            return satFormula;
        }

        Object expr = original;
//        long expr = Aig.simplify(original);
//        System.out.println("Creating Sat formula for " + getExprImpl().toString(expr));


        SatObject satFormula = new SatObject(null, SatStatus.Unknown);
        SolverGuardType solverGuardType = getExprImpl().getType(expr);

        List<Object> satChildren = new ArrayList<>();
        for (Object child: getExprImpl().getChildren(expr)) {
            SatObject satChild = createSatFormula(child);
            satChildren.add(satChild.formula);
        }

        switch(solverGuardType) {
            case TRUE:
                satFormula.formula = SatGuard.getSolver().constTrue();
                satFormula.status = SatStatus.Sat;
                break;
            case FALSE:
                satFormula.formula = SatGuard.getSolver().constFalse();
                satFormula.status = SatStatus.Unsat;
                break;
            case VARIABLE:
                satFormula.formula = SatGuard.getSolver().newVar(getExprImpl().toString(expr));
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
                throw new RuntimeException("Unexpected expr of type " + solverGuardType + " : " + expr);
        }
//        System.out.println("Returned Sat formula for " + getExprImpl().toString(expr));
        satTable.put(expr, satFormula);
        return satFormula;
    }

    private static boolean checkSat(SatExpr formula, SatObject satFormula) {
        switch (satFormula.status) {
            case Sat:
                return true;
            case Unsat:
                return false;
            default:
//            	System.out.println("Checking satisfiability of formula: " + Aig.toString(formula.expr));
                boolean isSat = SatGuard.getSolver().isSat(satFormula.formula);
                if (SearchLogger.getVerbosity() > 5) {
                	System.out.println("\t\tSAT ? [ " + getExprImpl().toString(formula.expr) + " ] :\t" + isSat);
                }
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

    private static SatObject createAigFormula(Object original) {
        if (aigTable.containsKey(original)) {
            return aigTable.get(original);
        }
        SatObject satFormula = new SatObject(original, SatStatus.Unknown);
        satFormula.status = ((Fraig)exprImpl).isSat((Long)original, Fraig.solveBTLimit);
        aigTable.put(original, satFormula);
        return satFormula;
    }

    public static boolean isSat(SatExpr formula) {
        SatObject satFormula;
        if (getExprType() == ExprLibType.Fraig && Fraig.useFraigSat) {
            satFormula = createAigFormula(formula.expr);
            switch (satFormula.status) {
                case Sat:
                    return true;
                case Unsat:
                    return false;
                default:
            }
        }
        satFormula = createSatFormula(formula.expr);
        return checkSat(formula, satFormula);
    }

    private static SatExpr newExpr(Object original) {
        return new SatExpr(original);
    }

    public static SatExpr ConstTrue() {
        return newExpr(getExprImpl().getTrue());
    }

    public static SatExpr ConstFalse() {
        return newExpr(getExprImpl().getFalse());
    }

    public static SatExpr NewVar(String name) {
        numVars++;
        return newExpr(getExprImpl().newVar(name));
    }

    public static SatExpr Not(SatExpr formula) {
        return newExpr(getExprImpl().not(formula.expr));
    }

    public static SatExpr And(SatExpr left, SatExpr right) {
        return newExpr(getExprImpl().and(left.expr, right.expr));
    }

    public static SatExpr Or(SatExpr left, SatExpr right) {
        return newExpr(getExprImpl().or(left.expr, right.expr));
    }

    public static void startSimplify() {
        if (getExprType() == ExprLibType.Iaig) {
            satTable.clear();
            aigTable.clear();
            ((Iaig)getExprImpl()).startSimplify();
        }
    }

    public static void stopSimplify() {
        if (getExprType() == ExprLibType.Iaig) {
            ((Iaig)getExprImpl()).stopSimplify();
        }
    }

    public static SatExpr Simplify(SatExpr formula) {
        Object simplified = formula.expr;
        switch(exprType) {
            case Iaig:
                simplified = getExprImpl().simplify(formula.expr);
        }
        formula.expr = simplified;
        return formula;
    }

    @Override
    public String toString() {
        return getExprImpl().toString(this.expr);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (!(o instanceof SatExpr)) return false;
        SatExpr that = (SatExpr) o;
        return getExprImpl().areEqual(this.expr, that.expr);
    }

    @Override
    public int hashCode() {
        return getExprImpl().getHashCode(this.expr);
    }
}
