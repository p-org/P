package psymbolic.valuesummary.bdd;

/**
 * Represents the backend engine that implements the interface to BDD Library
 */
public class BDDEngine {
    private static BDDEngine instance = new BDDEngine();

    // reference to the PJBDD based implementation
    private static PJBDDImpl bddImpl;

    // Make this constructor static so that the class cannot be instantiated
    private BDDEngine() {
        // use the pjbdd implementation without cbdd
        bddImpl = new PJBDDImpl(true);
    }

    /**
     * Get the singleton instance of this class
     * @return globally shared instance of the Bdd
     */
    public static BDDEngine getInstance() {
        return instance;
    }

    /**
     * Reset the global Bdd
     */
    public static void reset() {
        instance = new BDDEngine();
    }

    public BddGuard constFalse() {
        return new BddGuard(bddImpl.constFalse());
    }

    public BddGuard constTrue() {
        return new BddGuard(bddImpl.constTrue());
    }

    public boolean isFalse(BddGuard bdd) {
        return bddImpl.isFalse(bdd.getBdd());
    }

    public boolean isTrue(BddGuard bdd) {
        return bddImpl.isTrue(bdd.getBdd());
    }

    public BddGuard and(BddGuard left, BddGuard right) {
        return new BddGuard(bddImpl.and(left.getBdd(), right.getBdd()));
    }

    public BddGuard or(BddGuard left, BddGuard right) {
        return new BddGuard(bddImpl.or(left.getBdd(), right.getBdd()));
    }

    public BddGuard not(BddGuard bdd) {
        return new BddGuard(bddImpl.not(bdd.getBdd()));
    }

    public BddGuard implies(BddGuard left, BddGuard right) {
        return new BddGuard(bddImpl.implies(left.getBdd(), right.getBdd()));
    }

    public BddGuard ifThenElse(BddGuard cond, BddGuard thenClause, BddGuard elseClause) {
        return new BddGuard(bddImpl.ifThenElse(cond.getBdd(), thenClause.getBdd(), elseClause.getBdd()));
    }

    public BddGuard newVar() {
        return new BddGuard(bddImpl.newVar());
    }

    public String toString(BddGuard bdd) {
        if (bdd == null) return "null";
        if (bdd.isFalse()) return "false";
        if (bdd.isTrue()) return "true";
        return bddImpl.toString(bdd.getBdd());
    }
}
