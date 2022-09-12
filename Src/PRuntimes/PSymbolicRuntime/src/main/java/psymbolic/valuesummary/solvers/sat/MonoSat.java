package psymbolic.valuesummary.solvers.sat;

import monosat.Lit;
import monosat.Solver;
import psymbolic.runtime.statistics.SolverStats;
import java.util.HashMap;
import java.util.List;

/**
 * Represents the Sat implementation using MonoSAT
 */
public class MonoSat implements SatLib<Lit> {
    private Solver solver = null;
    private Lit valTrue;
    private Lit valFalse;
    private static HashMap<Lit, SatStatus> table = new HashMap<Lit, SatStatus>();

    public MonoSat() {
        System.out.println("Using MonoSAT version " + Solver.getVersion());
        solver = new Solver();
        valFalse = Lit.False;
        valTrue = Lit.True;
//    	System.out.println("Creating true/false constants");
    }

    public boolean checkSat(Lit formula) {
//    	System.out.println("Checking formula: " + toString(formula));
        boolean result = solver.solve(formula);
//    	System.out.println("Result: " + result);

        if (result) {
            SolverStats.isSatResult++;
            table.put(formula, SatStatus.Sat);
        } else {
            table.put(formula, SatStatus.Unsat);
        }
        return result;
    }

    public boolean isSat(Lit formula) {
        if (table.containsKey(formula)) {
            switch (table.get(formula)) {
                case Sat:
                    return true;
                case Unsat:
                    return false;
                default:
                    throw new RuntimeException("Expected cached query result to be SAT or UNSAT, got unknown for formula: " + toString(formula));
            }
        }
        SolverStats.isSatOperations++;
        return checkSat(formula);
    }

    public Lit constFalse() {
        return valFalse;
    }

    public Lit constTrue() {
        return valTrue;
    }

    public Lit and(List<Lit> children) {
        return solver.and(children);
    }

    public Lit or(List<Lit> children) {
        return solver.or(children);
    }

    public Lit not(Lit booleanFormula) {
        return solver.not(booleanFormula);
    }

    public Lit newVar(String name) {
        Lit t = new Lit(solver, name);
        return t;
    }

    public String toString(Lit booleanFormula) {
        return booleanFormula.toString();
    }

    public Lit fromString(String s) {
        if (s.equals("false")) {
            return constFalse();
        }
        if (s.equals("true")) {
            return constTrue();
        }
        throw new RuntimeException("Unsupported");
    }

    public int getNodeCount() {
        return solver.nVars();
    }

    public String getStats() {
        // TODO
        return "";
    }

    public void cleanup() {
        // TODO
    }

    public boolean areEqual(Lit left, Lit right) {
        return left.equals(right);
    }

}
