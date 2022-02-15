package psymbolic.valuesummary.solvers.sat;

import psymbolic.runtime.statistics.SATStats;
import org.sosy_lab.java_smt.api.*;

/**
 * Represents the backend engine that implements the interface to SAT Library
 */
public class SATEngine {
    private static SATEngine instance = new SATEngine();

    // reference to the JavaSmt based implementation
    private static JaveSmtImpl satImpl;

    // Make this constructor static so that the class cannot be instantiated
    private SATEngine() {
        // use the javasmt implementation
        satImpl = new JaveSmtImpl();
    }

    /**
     * Get the singleton instance of this class
     * @return globally shared instance of the Sat engine
     */
    public static SATEngine getInstance() {
        return instance;
    }

    /**
     * Reset the global Sat engine
     */
    public static void reset() {
        instance = new SATEngine();
    }

    public static void UnusedNodesCleanUp() {
        satImpl.UnusedNodeCleanUp();
    }

    public static int NodeCount() {
        return satImpl.getNodeCount();
    }

    public SatGuard constFalse() {
        return new SatGuard(satImpl.constFalse());
    }

    public SatGuard constTrue() {
        return new SatGuard(satImpl.constTrue());
    }

    public boolean isFalse(SatGuard formula) {
        SATStats.isFalseOperations++;
        if (satImpl.isFalse(formula.getFormula())) {
        	SATStats.isFalseResult++;
        	return true;
        } else {
        	return false;
        }
    }

    public boolean isTrue(SatGuard formula) {
        SATStats.isTrueOperations++;
        if (satImpl.isTrue(formula.getFormula())) {
        	SATStats.isTrueResult++;
        	return true;
        } else {
        	return false;
        }
    }

    public SatGuard and(SatGuard left, SatGuard right) {
        SATStats.andOperations++;
        return new SatGuard(satImpl.and(left.getFormula(), right.getFormula()));
    }

    public SatGuard or(SatGuard left, SatGuard right) {
        SATStats.orOperations++;
        return new SatGuard(satImpl.or(left.getFormula(), right.getFormula()));
    }

    public SatGuard not(SatGuard formula) {
        SATStats.notOperations++;
        return new SatGuard(satImpl.not(formula.getFormula()));
    }

    public SatGuard implies(SatGuard left, SatGuard right) {
        return new SatGuard(satImpl.implies(left.getFormula(), right.getFormula()));
    }

    public SatGuard ifThenElse(SatGuard cond, SatGuard thenClause, SatGuard elseClause) {
        return new SatGuard(satImpl.ifThenElse(cond.getFormula(), thenClause.getFormula(), elseClause.getFormula()));
    }

    public SatGuard newVar() {
        return new SatGuard(satImpl.newVar());
    }

    public static String toString(SatGuard formula) {
        if (formula == null) return "null";
        if (formula.isFalse()) return "false";
        if (formula.isTrue()) return "true";
        return satImpl.toString(formula.getFormula());
    }

    public int getVarCount() {
        return satImpl.getVarCount();
    }

    public int getNodeCount() {
        return satImpl.getNodeCount();
    }

    public String getStats() {
        return satImpl.getStats() + "\n" + "Node Count:" + satImpl.getNodeCount();
    }
}
