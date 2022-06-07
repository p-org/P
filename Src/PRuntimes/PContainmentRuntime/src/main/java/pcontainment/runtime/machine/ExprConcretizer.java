package pcontainment.runtime.machine;

import com.microsoft.z3.*;
import p.runtime.values.PBool;
import p.runtime.values.PFloat;
import p.runtime.values.PInt;
import p.runtime.values.PValue;

public class ExprConcretizer {

    public static boolean isConcrete(Expr<?> expr) {
        if (expr instanceof BoolExpr) {
            return expr.isTrue() || expr.isFalse();
        } else if (expr instanceof IntExpr) {
            return expr.isNumeral();
        } else if (expr instanceof RealExpr) {
            return expr.isRatNum();
        }
        // TODO: detect concrete values for other types
        return false;
    }

    public static PValue<?> concretize(Expr<?> expr) {
        if (expr instanceof BoolExpr) {
            return getBool((BoolExpr) expr);
        }else if (expr instanceof IntExpr) {
            return getInt((IntNum) expr);
        } else if (expr instanceof RealExpr) {
            return getFloat((RatNum) expr);
        }
        throw new RuntimeException("Cannot concretize expression " + expr);
    }

    public static PInt getInt(IntNum expr) {
        return new PInt(expr.getInt());
    }

    public static PBool getBool(BoolExpr expr) {
        if (expr.isTrue()) return new PBool(true);
        if (expr.isFalse()) return new PBool(false);
        throw new RuntimeException("Tried to concretize symbolic Boolean with nondet value");
    }

    public static PFloat getFloat(RatNum expr) {
        return new PFloat(expr.getBigIntNumerator().doubleValue() / expr.getBigIntDenominator().doubleValue());
    }
}
