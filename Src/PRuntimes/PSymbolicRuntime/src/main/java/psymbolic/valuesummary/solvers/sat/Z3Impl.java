package psymbolic.valuesummary.solvers.sat;

import psymbolic.runtime.logger.SearchLogger;
import psymbolic.runtime.statistics.SolverStats;
import com.microsoft.z3.*;

import java.util.HashMap;
import java.util.List;

/**
 * Represents the Sat implementation using Z3
 */
public class Z3Impl implements SatLib<BoolExpr> {
    private HashMap<String, String> config;
    private Context context;
    private Params simplifyParams;
    private Solver solver;
    private Sort boolType;
    private BoolExpr valTrue;
    private BoolExpr valFalse;
    private int printCount = 0;
    private static HashMap<BoolExpr, SatStatus> table = new HashMap<BoolExpr, SatStatus>();

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
        simplifyParams.add("elim_and", true);
        simplifyParams.add("flat", true);
        simplifyParams.add("local_ctx", true);

        boolType = context.mkBoolSort();
        valFalse = newTerm(context.mkFalse());
        valTrue = newTerm(context.mkTrue());
    	System.out.println("Creating true/false constants");

        solver = context.mkSolver("QF_UF");
//    	throw new RuntimeException("Debug point reached");
    }

    private BoolExpr newTerm(BoolExpr t) {
        BoolExpr tSimple = t;
        if (SearchLogger.getVerbosity() <= 4) {
            tSimple = (BoolExpr) t.simplify(simplifyParams);
//        System.out.println("\toriginal term  : " + toString(t));
//        System.out.println("\tsimplified term: " + toString(tSimple));
        }
        return tSimple;
    }

    public void toSmtLib(String status, BoolExpr formula) {
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
            table.put(formula, SatStatus.Sat);
            return true;
        } else if (result == Status.UNSATISFIABLE) {
            table.put(formula, SatStatus.Unsat);
//            solver.add(not(formula));

            if (SearchLogger.getVerbosity() > 4) {
                if (formula != valFalse && printCount < 10) {
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

    public BoolExpr constFalse() {
        return valFalse;
    }

    public BoolExpr constTrue() {
        return valTrue;
    }

    public BoolExpr and(List<BoolExpr> children) {
        BoolExpr[] c = children.toArray(new BoolExpr[children.size()]);
        return newTerm(context.mkAnd(c));
    }

    public BoolExpr or(List<BoolExpr> children) {
        BoolExpr[] c = children.toArray(new BoolExpr[children.size()]);
        return newTerm(context.mkOr(c));
    }

    public BoolExpr not(BoolExpr booleanFormula) {
        return newTerm(context.mkNot(booleanFormula));
    }

    public BoolExpr newVar(String name) {
        BoolExpr t;
        t = newTerm(context.mkBoolConst(name));
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
