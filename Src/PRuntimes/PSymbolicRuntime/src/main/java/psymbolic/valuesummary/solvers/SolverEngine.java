package psymbolic.valuesummary.solvers;

import psymbolic.valuesummary.solvers.SolverGuard;

/**
 * Represents the generic backend engine
 */
public class SolverEngine {
    // Make this constructor static so that the class cannot be instantiated
    public SolverEngine() {}

    public static void ResetEngines() {
        SolverGuard.resetSolver();
    }

    public static void CleanUpEngines() {
        SolverGuard.cleanup();
    }

}
