package psymbolic.valuesummary.solvers;

import psymbolic.valuesummary.solvers.bdd.BDDEngine;
import psymbolic.valuesummary.solvers.sat.SATEngine;

/**
 * Represents the generic backend engine
 */
public abstract class SolverEngine {
    // Make this constructor static so that the class cannot be instantiated
    public SolverEngine() {}

    public static void ResetEngines() {
        BDDEngine.reset();
        SATEngine.reset();
    }

    public static void CleanUpEngines() {
        BDDEngine.UnusedNodesCleanUp();
        SATEngine.UnusedNodesCleanUp();
    }

}
