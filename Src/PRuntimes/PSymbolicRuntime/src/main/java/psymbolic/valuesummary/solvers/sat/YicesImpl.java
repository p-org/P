package psymbolic.valuesummary.solvers.sat;

import psymbolic.runtime.statistics.SolverStats;
import psymbolic.valuesummary.solvers.SolverLib;
import psymbolic.valuesummary.solvers.SolverStatus;
import com.sri.yices.*;
import java.lang.Integer;
import java.util.HashMap;

/**
 * Represents the Sat implementation using Yices
 */
public class YicesImpl implements SolverLib<Integer> {
    private long config;
    private long context;
    private long param;
    private int boolType;
    private Integer valTrue;
    private Integer valFalse;    
    private long idx = 0;
    private static HashMap<Integer, SolverStatus> table = new HashMap<Integer, SolverStatus>();

    // Yices2 status codes
    public static final int YICES_STATUS_IDLE = 0;
    public static final int YICES_STATUS_SEARCHING = 1;
    public static final int YICES_STATUS_UNKNOWN = 2;
    public static final int YICES_STATUS_SAT = 3;
    public static final int YICES_STATUS_UNSAT = 4;
    public static final int YICES_STATUS_INTERRUPTED = 5;
    public static final int YICES_STATUS_ERROR = 6;

    public YicesImpl() {
    	int status;
    	
    	System.out.println("Using Yices version " + Yices.version());

    	config = Yices.newConfig();
        status = Yices.defaultConfigForLogic(config, "NONE");
        status = Yices.setConfig(config, "mode", "multi-checks");
//    	System.out.println("Creating config for logic NONE, status: " + status);

        context = Yices.newContext(config);
        status = Yices.contextEnableOption(context, "var-elim");
        status = Yices.contextEnableOption(context, "bvarith-elim");
        status = Yices.contextEnableOption(context, "flatten");
//    	System.out.println("Creating context");
    	
    	param = Yices.newParamRecord();
    	Yices.defaultParamsForContext(context, param);
//    	System.out.println("Creating default params");
    	
    	boolType = Yices.boolType();
    	valFalse = Yices.mkFalse();
    	valTrue = Yices.mkTrue();
//    	System.out.println("Creating true/false constants");
    }
    
    public boolean checkSat(Integer formula) {
        int result = YICES_STATUS_UNKNOWN;
//    	System.out.println("Checking formula: " + toString(formula));
//        result = Yices.checkFormula(formula, "QF_UF", "NULL", null);
    	result = Yices.checkContextWithAssumptions(context, param, new int[]{formula});
//    	System.out.println("Result: " + result);
//    	System.out.println("Yices status: " + Yices.errorString());
//    	throw new RuntimeException("Debug point reached");

        switch (result) {
            case YICES_STATUS_SAT:
                SolverStats.isSatResult++;
                table.put(formula, SolverStatus.SolverSat);
                return true;
            case YICES_STATUS_UNSAT:
                table.put(formula, SolverStatus.SolverUnsat);
//                Yices.assertFormula(context, not(formula));
                return false;
            default:
                throw new RuntimeException("Yices returned query result: " + result + " with error: " + Yices.errorString());
        }
    }

    public boolean isSat(Integer formula) {
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

    public Integer constFalse() {
        return valFalse;
    }

    public Integer constTrue() {
        return valTrue;
    }

    public Integer and(Integer left, Integer right) {
        return Yices.and(left, right);
    }

    public Integer or(Integer left, Integer right) {
        return Yices.or(left, right);
    }

    public Integer not(Integer booleanFormula) {
        return Yices.not(booleanFormula);
    }

    public Integer implies(Integer left, Integer right) {
        return Yices.implies(left, right);
    }

    public Integer ifThenElse(Integer cond, Integer thenClause, Integer elseClause) {
        return Yices.or(Yices.and(cond, thenClause),
        		Yices.and(Yices.not(cond), elseClause));
    }

    public Integer newVar() {
        int t, status;
        t = Yices.newUninterpretedTerm(boolType);
        Yices.setTermName(t, "x" + idx++);
//    	System.out.println("\tnew variable: " + Yices.getTermName(t));
        return t;
    }

    public String toString(Integer booleanFormula) {
//        return Yices.termToString(booleanFormula);
        return Yices.termToString(booleanFormula, 80, 1);
    }

    public Integer fromString(String s) {
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
//        return Yices.yicesNumTerms();
        return table.size();
    }

    public String getStats() {
    	// TODO
        return "";
    }

    public void cleanup() {
    	// TODO
    }

    public boolean areEqual(Integer left, Integer right) {
        return left.equals(right);
    }

}
