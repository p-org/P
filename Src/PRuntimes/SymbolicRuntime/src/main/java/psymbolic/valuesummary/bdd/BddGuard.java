package psymbolic.valuesummary.bdd;

import lombok.Getter;
import org.sosy_lab.pjbdd.api.DD;

import java.util.List;

/**
    Represents the BDD based implementation of Guard
 */
public class BddGuard {

    @Getter
    private final DD bdd;

    private static final BDDEngine bddEngine = BDDEngine.getInstance();

    public BddGuard(DD bdd)
    {
        this.bdd = bdd;
    }

    public static BddGuard constFalse() {
        return bddEngine.constFalse();
    }

    public static BddGuard constTrue() {
        return bddEngine.constTrue();
    }

    public boolean isFalse() {
        return bddEngine.isFalse(this);
    }

    public boolean isTrue() {
        return bddEngine.isTrue(this);
    }

    public BddGuard and(BddGuard other) {
        return bddEngine.and(this, other);
    }

    public BddGuard or(BddGuard other) {
        return bddEngine.or(this, other);
    }

    public BddGuard implies(BddGuard other) {
        return bddEngine.implies(this, other);
    }

    public BddGuard not() {
        return bddEngine.not(this);
    }

    public static BddGuard orMany(List<BddGuard> bddGuards) {
        return bddGuards.stream().reduce(BddGuard.constFalse(), BddGuard::or);
    }

    public BddGuard ifThenElse(BddGuard thenCase, BddGuard elseCase) {
        return bddEngine.ifThenElse(this, thenCase, elseCase);
    }

    public static BddGuard newVar() {
        return bddEngine.newVar();
    }

    @Override
    public String toString() {
        return bdd.toString();
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (!(o instanceof BddGuard)) return false;
        BddGuard bddGuard = (BddGuard) o;
        return bdd.equals(bddGuard.bdd);
    }

    @Override
    public int hashCode() {
        return bdd.hashCode();
    }
}
