package psymbolic.valuesummary.solvers.sat;

import com.microsoft.z3.BoolExpr;
import psymbolic.runtime.logger.SearchLogger;
import psymbolic.valuesummary.solvers.SolverType;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Arrays;

public class SatExpr {
    public static int numVars = 0;
    public static HashMap<Long, SatObject> table = new HashMap<Long, SatObject>();
    public static HashMap<Long, SatObject> aigTable = new HashMap<Long, SatObject>();
    private long expr;

    public SatExpr(long expr) {
        this.expr = expr;
    }

    private static SatObject createSatFormula(long original) {
        if (table.containsKey(original)) {
            return table.get(original);
        } else if (SatGuard.getSolverType() == SolverType.ABC) {
            if (aigTable.containsKey(original)) {
                return aigTable.get(original);
            }
            SatObject satFormula = new SatObject(original, SatStatus.Unknown);
            table.put(original, satFormula);
            return satFormula;
        }

        long expr = original;
//        long expr = Aig.simplify(original);
//        System.out.println("Creating Sat formula for " + toString(expr));


        SatObject satFormula = new SatObject(null, SatStatus.Unknown);
        ExprType exprType = Aig.getType(expr);

        List<Object> satChildren = new ArrayList<>();
        for (long child: Aig.getChildren(expr)) {
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
                satFormula.formula = SatGuard.getSolver().newVar(Aig.toString(expr));
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

    private static boolean checkSat(SatExpr formula, SatObject satFormula) {
        switch (satFormula.status) {
            case Sat:
                return true;
            case Unsat:
                return false;
            default:
//            	System.out.println("Checking satisfiability of formula: " + Aig.toString(formula.expr));
                boolean isSat = SatGuard.getSolver().isSat(satFormula.formula);
                if (SearchLogger.getVerbosity() > 4) {
                	System.out.println("\t\tSAT ? [ " + Aig.toString(formula.expr) + " ] :\t" + isSat);
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

    private static SatObject createAigFormula(long original) {
        if (aigTable.containsKey(original)) {
            return aigTable.get(original);
        }
        SatObject satFormula = new SatObject(original, SatStatus.Unknown);
        satFormula.status = Aig.isSat(original, Aig.nBTLimit);
        aigTable.put(original, satFormula);
        return satFormula;
    }

    public static boolean isSat(SatExpr formula) {
        SatObject satFormula;
//        satFormula = createAigFormula(formula.expr);
//        switch (satFormula.status) {
//            case Sat:
////                satFormula = createSatFormula(formula.expr);
////                if (!checkSat(formula, satFormula)) {
////                    if (SatGuard.getSolverType() == SolverType.Z3) {
////                        ((Z3Impl) SatGuard.getSolver()).toSmtLib("unknown", (BoolExpr) satFormula.formula);
////                    }
////                    System.out.println("Aig says SAT while Solver says UNSAT");
////                    Aig.isSat(formula.expr, SatExpr.nBTLimit);
////                    throw new RuntimeException("Conflicting SAT result for formula " + formula);
////                }
//                return true;
//            case Unsat:
////                satFormula = createSatFormula(formula.expr);
////                if (checkSat(formula, satFormula)) {
////                    if (SatGuard.getSolverType() == SolverType.Z3) {
////                        ((Z3Impl) SatGuard.getSolver()).toSmtLib("unknown", (BoolExpr) satFormula.formula);
////                    }
////                    System.out.println("Aig says UNSAT while Solver says SAT");
////                    Aig.isSat(formula.expr, SatExpr.nBTLimit);
////                    throw new RuntimeException("Conflicting SAT result for formula " + formula);
////                }
//                return false;
//            default:
//        }
        satFormula = createSatFormula(formula.expr);
        return checkSat(formula, satFormula);
    }

    private static SatExpr newExpr(long original) {
        return new SatExpr(original);
    }

    public static SatExpr ConstTrue() {
        return newExpr(Aig.getTrue());
    }

    public static SatExpr ConstFalse() {
        return newExpr(Aig.getFalse());
    }

    public static SatExpr NewVar() {
        return newExpr(Aig.newVar("x" + numVars++));
    }

    public static SatExpr Not(SatExpr formula) {
        return newExpr(Aig.not(formula.expr));
    }

    public static SatExpr And(SatExpr left, SatExpr right) {
        return newExpr(Aig.and(left.expr, right.expr));
    }

    public static SatExpr Or(SatExpr left, SatExpr right) {
        return newExpr(Aig.or(left.expr, right.expr));
    }

    @Override
    public String toString() {
        return Aig.toString(this.expr);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (!(o instanceof SatExpr)) return false;
        SatExpr that = (SatExpr) o;
        return Aig.areEqual(this.expr, that.expr);
    }

    @Override
    public int hashCode() {
        return Aig.getHashCode(this.expr);
    }
}
