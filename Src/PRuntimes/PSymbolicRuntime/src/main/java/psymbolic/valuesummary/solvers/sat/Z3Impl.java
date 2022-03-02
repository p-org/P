package psymbolic.valuesummary.solvers.sat;

import psymbolic.runtime.logger.SearchLogger;
import psymbolic.runtime.statistics.SolverStats;
import psymbolic.valuesummary.solvers.SolverLib;
import psymbolic.valuesummary.solvers.SolverStatus;
import com.microsoft.z3.*;

import java.util.HashMap;

/**
 * Represents the Sat implementation using Z3
 */
public class Z3Impl implements SolverLib<BoolExpr> {
    private HashMap<String, String> config;
    private Context context;
    private Params simplifyParams;
    private Solver solver;
    private Sort boolType;
    private BoolExpr valTrue;
    private BoolExpr valFalse;
    private long idx = 0;
    private int printCount = 0;
    private static HashMap<BoolExpr, SolverStatus> table = new HashMap<BoolExpr, SolverStatus>();

    public Z3Impl() {
    	System.out.println("Using Z3 version " + Version.getString());

        config = new HashMap<String, String>();
        config.put("proof", "false");
        config.put("debug_ref_count", "false");
        config.put("trace", "false");
        config.put("auto-config", "true");
        config.put("model", "false");
        config.put("model_validate", "false");
        config.put("unsat_core", "false");

        context = new Context(config);

        simplifyParams = context.mkParams();
//        if (SearchLogger.getVerbosity() <= 2)
        {
            simplifyParams.add("elim_and", true);
            simplifyParams.add("flat", true);
            simplifyParams.add("local_ctx", true);
        }

        boolType = context.mkBoolSort();
        valFalse = newTerm(context.mkFalse());
        valTrue = newTerm(context.mkTrue());
    	System.out.println("Creating true/false constants");

        solver = context.mkSolver("QF_UF");
//    	throw new RuntimeException("Debug point reached");
    }

    private BoolExpr newTerm(BoolExpr t) {
        BoolExpr tSimple = (BoolExpr) t.simplify(simplifyParams);
//        System.out.println("\toriginal term  : " + toString(t));
//        System.out.println("\tsimplified term: " + toString(tSimple));
        return tSimple;
//        return t;
    }

    private void toSmtLib(String status, BoolExpr formula) {
        String s, name;

        printCount++;
        name = "Query " + printCount;
        BoolExpr assumptions[] = {valTrue};
        s = context.benchmarkToSMTString(name, "QF_UF", status, "", assumptions, formula);

        SearchLogger.log("");
        SearchLogger.log(s);
        SearchLogger.log("");
//    	throw new RuntimeException("Debug point reached");
    }

    public boolean checkSat(BoolExpr formula) {
        Status result = Status.UNKNOWN;
//    	System.out.println("Checking formula: " + toString(formula));
        result = solver.check(formula);
//    	System.out.println("Result: " + result);
//    	throw new RuntimeException("Debug point reached");

        if (result == Status.SATISFIABLE) {
            SolverStats.isSatResult++;
            table.put(formula, SolverStatus.SolverSat);
            return true;
        } else if (result == Status.UNSATISFIABLE) {
            table.put(formula, SolverStatus.SolverUnsat);
//            solver.add(not(formula));

            if (SearchLogger.getVerbosity() > 2) {
                if (formula != valFalse && printCount < 100) {
                    toSmtLib("unsat", formula);
                }
            }
            return false;
        } else {
            throw new RuntimeException("Z3 returned query result: " + result);
        }
    }

    public boolean isSat(BoolExpr formula) {
        if (table.containsKey(formula)) {
            switch (table.get(formula)) {
                case SolverSat:
                    return true;
                case SolverUnsat:
                    return false;
                default:
                    throw new RuntimeException("Expected cached query result to be SAT or UNSAT, got unknown for formula: " + toString(formula));
            }
        }
        SolverStats.isSatOperations++;
        return checkSat(formula);
    }

    public BoolExpr constFalse() {
        return valFalse;
    }

    public BoolExpr constTrue() {
        return valTrue;
    }

    public BoolExpr and(BoolExpr left, BoolExpr right) {
        return newTerm(context.mkAnd(left, right));
    }

    public BoolExpr or(BoolExpr left, BoolExpr right) {
        return newTerm(context.mkOr(left, right));
    }

    public BoolExpr not(BoolExpr booleanFormula) {
        return newTerm(context.mkNot(booleanFormula));
    }

    public BoolExpr implies(BoolExpr left, BoolExpr right) {
        return newTerm(context.mkImplies(left, right));
    }

    public BoolExpr ifThenElse(BoolExpr cond, BoolExpr thenClause, BoolExpr elseClause) {
        return newTerm(context.mkOr(context.mkAnd(cond, thenClause),
                context.mkAnd(context.mkNot(cond), elseClause)));
    }

    public BoolExpr newVar() {
        BoolExpr t;
        t = newTerm(context.mkBoolConst("x" + idx++));
        return t;
    }

    public String toString(BoolExpr booleanFormula) {
        return String.format("%d\t%.80s", booleanFormula.getId(), booleanFormula.toString());
    }

    public BoolExpr fromString(String s) {
        if (s.equals("false")) {
            return constFalse();
        }
        if (s.equals("true")) {
            return constTrue();
        }
        throw new RuntimeException("Unsupported");
    }

    public int getVarCount() {
        return (int)idx;
    }

    public int getNodeCount() {
        return table.size();
    }

    public String getStats() {
        // TODO
        return "";
    }

    public void cleanup() {
        // TODO
    }

    public boolean areEqual(BoolExpr left, BoolExpr right) {
        return left.equals(right);
    }

}
