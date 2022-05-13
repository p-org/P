package psymbolic.valuesummary.solvers.sat;

import io.github.cvc5.api.CVC5ApiException;
import io.github.cvc5.api.Kind;
import io.github.cvc5.api.Result;
import io.github.cvc5.api.Solver;
import io.github.cvc5.api.Sort;
import io.github.cvc5.api.Term;
import psymbolic.runtime.statistics.SolverStats;

import java.util.HashMap;
import java.util.List;

/**
 * Represents the Sat implementation using CVC5
 */
public class CVC5Impl implements SatLib<Term> {
    private Solver solver;
    private Sort boolType;
    private Term valTrue;
    private Term valFalse;
    private static HashMap<Long, SatStatus> table = new HashMap<Long, SatStatus>();

    public CVC5Impl() {
    	System.out.println("Using CVC5");

        try {
            solver = new Solver();
            solver.setLogic("QF_UF");

            solver.setOption("repeat-simp", "true");
            solver.setOption("ite-simp", "true");
            solver.setOption("on-repeat-ite-simp", "true");
            solver.setOption("ext-rew-prep", "true");
            solver.setOption("simp-with-care", "true");
            solver.setOption("early-ite-removal", "true");
//            solver.setOption("unconstrained-simp", "true");

//            solver.setOption("bitblast", "eager");
//            solver.setOption("bool-to-bv", "all");

//            System.out.println("Options:");
//            for (String option: solver.getOptionNames()) {
//                try {
//                    System.out.println("\t" + option + ":\t" + solver.getOption(option));
////                  System.out.println("\t\t" + solver.getOptionInfo(option));
//                } finally {
//                }
//            }

            boolType = solver.getBooleanSort();
            valFalse = newTerm(solver.mkBoolean(false));
            valTrue = newTerm(solver.mkBoolean(true));
            System.out.println("Creating true/false constants");
        } catch (CVC5ApiException e) {
            e.printStackTrace();
            throw new RuntimeException("Invalid configuration for SMT");
        }
    }

    private Term newTerm(Term t) {
        Term tSimple = solver.simplify(t);
//        System.out.println("\toriginal term  : " + toString(t));
//        System.out.println("\tsimplified term: " + toString(tSimple));
//    	throw new RuntimeException("Debug point reached");
        return tSimple;
//        return t;
    }

    public boolean checkSat(Term formula) {
//    	System.out.println("Checking formula: " + toString(formula));
    	Result result = solver.checkSatAssuming(formula);
//    	System.out.println("Result: " + result);
//    	throw new RuntimeException("Debug point reached");

        if (result.isSat()) {
            SolverStats.isSatResult++;
            table.put(formula.getId(), SatStatus.Sat);
            return true;
        } else if (result.isUnsat()) {
            table.put(formula.getId(), SatStatus.Unsat);
            return false;
        } else {
            throw new RuntimeException("CVC5 returned query result: " + result + " with explanation: " + result.getUnknownExplanation());
        }
    }

    public boolean isSat(Term formula) {
        if (table.containsKey(formula.getId())) {
            switch (table.get(formula.getId())) {
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

    public Term constFalse() {
        return valFalse;
    }

    public Term constTrue() {
        return valTrue;
    }

    public Term and(List<Term> children) {
        Term[] c = children.toArray(new Term[children.size()]);
        return newTerm(solver.mkTerm(Kind.AND, c));
    }

    public Term or(List<Term> children) {
        Term[] c = children.toArray(new Term[children.size()]);
        return newTerm(solver.mkTerm(Kind.OR, c));
    }

    public Term not(Term booleanFormula) {
        return newTerm(solver.mkTerm(Kind.NOT, booleanFormula));
    }

    public Term implies(Term left, Term right) {
//        return or(not(left), right);
        return newTerm(solver.mkTerm(Kind.IMPLIES, left, right));
    }

    public Term ifThenElse(Term cond, Term thenClause, Term elseClause) {
//        return or(and(cond, thenClause), and(not(cond), elseClause));
        return newTerm(solver.mkTerm(Kind.ITE, cond, thenClause, elseClause));
    }

    public Term newVar(String name) {
        Term t = newTerm(solver.mkConst(boolType, name));
//    	System.out.println("\tnew variable: " + t);
        return t;
    }

    public String toString(Term booleanFormula) {
        return String.format("%d\t%.80s", booleanFormula.getId(), booleanFormula.toString());
    }

    public Term fromString(String s) {
        if (s.equals("false")) {
            return constFalse();
        }
        if (s.equals("true")) {
            return constTrue();
        }
        throw new RuntimeException("Unsupported");
    }

    public int getNodeCount() {
        return table.size();
    }

    public String getStats() {
    	// TODO
        return solver.getStatistics().toString();
    }

    public void cleanup() {
    	// TODO
    }

    public boolean areEqual(Term left, Term right) {
        return left.getId() == right.getId();
//        return left.equals(right);
    }
}
