package psymbolic.valuesummary.solvers.sat.expr;

import psymbolic.valuesummary.solvers.SolverGuardType;

import java.util.*;

public class NativeExpr implements ExprLib<NativeObject> {
    public NativeExpr() {
        reset();
    }

    public void reset() {
        NativeObject.simplifyTable.clear();
    }

    public NativeObject getTrue() {
        return NativeObject.getTrue();
    }

    public NativeObject getFalse() {
        return NativeObject.getFalse();
    }

    public NativeObject newVar(String name) {
        return NativeObject.newVar(name);
    }

    public NativeObject not(NativeObject child) {
        return NativeObject.not(child);
    }

    public NativeObject and(NativeObject childA, NativeObject childB) {
        return NativeObject.and(Arrays.asList(childA, childB));
    }

    public NativeObject or(NativeObject childA, NativeObject childB) {
        return NativeObject.or(Arrays.asList(childA, childB));
    }

    public NativeObject simplify(NativeObject formula) {
        return formula;
    }

    public SolverGuardType getType(NativeObject formula) {
        return formula.getType();
    }

    public List<NativeObject> getChildren(NativeObject formula) {
        return formula.getChildren();
    }

    public String toString(NativeObject formula) {
        return formula.toString();
    }

    public String getStats() {
        return "";
    }

    public int getExprCount() {
        return NativeObject.getExprCount();
    }

    public boolean areEqual(NativeObject left, NativeObject right) {
        return left.equals(right);
    }

    public int getHashCode(NativeObject formula) {
        return formula.hashCode();
    }

}
