package psymbolic.valuesummary.solvers.sat;

import lombok.Getter;
import org.sosy_lab.java_smt.api.*;

import java.util.List;

/**
    Represents the SAT based implementation of Guard
 */
public class SatGuard {

    @Getter
    private final BooleanFormula formula;

    private static final SATEngine satEngine = SATEngine.getInstance();

    public SatGuard(BooleanFormula formula)
    {
        this.formula = formula;
    }

    public static SatGuard constFalse() {
        return satEngine.constFalse();
    }

    public static SatGuard constTrue() {
        return satEngine.constTrue();
    }

    public boolean isFalse() {
        return satEngine.isFalse(this);
    }

    public boolean isTrue() {
        return satEngine.isTrue(this);
    }

    public SatGuard and(SatGuard other) {
        return satEngine.and(this, other);
    }

    public SatGuard or(SatGuard other) {
        return satEngine.or(this, other);
    }

    public SatGuard implies(SatGuard other) {
        return satEngine.implies(this, other);
    }

    public SatGuard not() {
        return satEngine.not(this);
    }

    public static SatGuard orMany(List<SatGuard> satGuards) {
        return satGuards.stream().reduce(SatGuard.constFalse(), SatGuard::or);
    }

    public SatGuard ifThenElse(SatGuard thenCase, SatGuard elseCase) {
        return satEngine.ifThenElse(this, thenCase, elseCase);
    }

    public static SatGuard newVar() {
        return satEngine.newVar();
    }

    @Override
    public String toString() {
        return satEngine.toString(this);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (!(o instanceof SatGuard)) return false;
        SatGuard SatGuard = (SatGuard) o;
        return formula.equals(SatGuard.formula);
    }

    @Override
    public int hashCode() {
        return formula.hashCode();
    }
}
