package psymbolic.valuesummary.solvers.bdd;

import psymbolic.runtime.statistics.BDDStats;

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
        bddImpl = new PJBDDImpl(false);
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

    public static void UnusedNodesCleanUp() {
        bddImpl.UnusedNodeCleanUp();
    }

    public static int NodeCount() {
        return bddImpl.getNodeCount();
    }

    public BddGuard constFalse() {
        return new BddGuard(bddImpl.constFalse());
    }

    public BddGuard constTrue() {
        return new BddGuard(bddImpl.constTrue());
    }

    public boolean isFalse(BddGuard bdd) {
        BDDStats.isFalseOperations++;
        if (bddImpl.isFalse(bdd.getBdd())) {
        	BDDStats.isFalseResult++;
        	return true;
        } else {
        	return false;
        }
    }

    public boolean isTrue(BddGuard bdd) {
        BDDStats.isTrueOperations++;
        if (bddImpl.isTrue(bdd.getBdd())) {
        	BDDStats.isTrueResult++;
        	return true;
        } else {
        	return false;
        }
    }

    public BddGuard and(BddGuard left, BddGuard right) {
        BDDStats.andOperations++;
        return new BddGuard(bddImpl.and(left.getBdd(), right.getBdd()));
    }

    public BddGuard or(BddGuard left, BddGuard right) {
        BDDStats.orOperations++;
        return new BddGuard(bddImpl.or(left.getBdd(), right.getBdd()));
    }

    public BddGuard not(BddGuard bdd) {
        BDDStats.notOperations++;
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

    public static String toString(BddGuard bdd) {
        if (bdd == null) return "null";
        if (bdd.isFalse()) return "false";
        if (bdd.isTrue()) return "true";
        return bddImpl.toString(bdd.getBdd());
    }

    public int getVarCount() {
        return bddImpl.getVarCount();
    }

    public int getNodeCount() {
        return bddImpl.getNodeCount();
    }

    public String getStats() {
        return bddImpl.getBDDStats() + "\n" + "Node Count:" + bddImpl.getNodeCount();
    }
}
