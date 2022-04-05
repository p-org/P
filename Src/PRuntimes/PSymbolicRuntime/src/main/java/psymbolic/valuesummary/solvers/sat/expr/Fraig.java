package psymbolic.valuesummary.solvers.sat.expr;
import com.berkeley.abc.Abc;
import psymbolic.valuesummary.solvers.SolverTrueStatus;
import psymbolic.valuesummary.solvers.SolverGuardType;
import psymbolic.valuesummary.solvers.sat.SatStatus;

import java.io.ByteArrayOutputStream;
import java.io.PrintStream;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;

public class Fraig implements ExprLib<Long> {
    private boolean hasStarted = false;
    public long network;
    private long params;
    private HashMap<String, Long> namedNodes = new HashMap<String, Long>();
    private HashMap<Long, String> varNames = new HashMap<Long, String>();
    public HashSet<Integer> idSet = new HashSet<Integer>();
    public static int isSatOperations = 0;
    public static int isSatResult = 0;
    public static int createBTLimit = 500;
    public static int solveBTLimit = 10;
    public static boolean useFraigSat = false;
    private static boolean useFraigFull = false;

    public Fraig() {
        reset();
    }

    public void reset() {
        resetAig();
    }

    public void resetAig() {
        if (hasStarted) {
            System.out.println("Resetting FRAIG");
            Abc.Fraig_ManFree(network);
        }
        hasStarted = true;

        System.out.println("Setting FRAIG Parameters");
        params = -1;
        if (useFraigFull) {
//            useFraigSat = true;
//            createBTLimit = -1;
//            solveBTLimit = -1;
            params = Abc.Fraig_ParamsGetDefaultFull();
        } else {
            params = Abc.Fraig_ParamsGetDefault();
            Abc.Fraig_ParamsSet_nPatsRand(params, 2048);
            Abc.Fraig_ParamsSet_nPatsDyna(params, 2048);
            Abc.Fraig_ParamsSet_nBTLimit(params, createBTLimit);
            Abc.Fraig_ParamsSet_nSeconds(params, 20);
            Abc.Fraig_ParamsSet_fFuncRed(params, 1);
            Abc.Fraig_ParamsSet_fFeedBack(params, 1);
            Abc.Fraig_ParamsSet_fDist1Pats(params, 1);
            Abc.Fraig_ParamsSet_fDoSparse(params, 1);
            Abc.Fraig_ParamsSet_fChoicing(params, 0);
            Abc.Fraig_ParamsSet_fTryProve(params, 0);
            Abc.Fraig_ParamsSet_fVerbose(params, 0);
            Abc.Fraig_ParamsSet_fVerboseP(params, 0);
            Abc.Fraig_ParamsSet_fInternal(params, 0);
            Abc.Fraig_ParamsSet_nConfLimit(params, 0);
            Abc.Fraig_ParamsSet_nInspLimit(params, 0);
        }

        System.out.println("Creating FRAIG Network");
        network = Abc.Fraig_ManCreate(params);
//        network = Abc.Fraig_ManCreate(-1);

        namedNodes.clear();
        varNames.clear();
        idSet.clear();

//        debug();
    }

    private void debug() {
        long fAig = network;
        long exprTrue = Abc.Fraig_ManReadConst1(network);
        long exprFalse = Abc.Fraig_Not(Abc.Fraig_ManReadConst1(network));

        System.out.println( "Created true with id: " + Abc.Fraig_NodeReadNum(exprTrue));
        System.out.println( "Created false with id: " + Abc.Fraig_NodeReadNum(exprFalse));

        long fObjAnd2;
        fObjAnd2 = Abc.Fraig_NodeAnd(fAig, exprTrue, exprFalse);
        System.out.println( "Created t^f with id: " + Abc.Fraig_NodeReadNum(fObjAnd2));

        long fObjA, fObjB, fObjC;
        fObjA = Abc.Fraig_ManReadIthVar(fAig, 0);
        fObjB = Abc.Fraig_ManReadIthVar(fAig, 1);
        fObjC = Abc.Fraig_ManReadIthVar(fAig, 2);

        System.out.println( "Created PI with id: " + Abc.Fraig_NodeReadNum(fObjA));
        System.out.println( "Created PI with id: " + Abc.Fraig_NodeReadNum(fObjB));
        System.out.println( "Created PI with id: " + Abc.Fraig_NodeReadNum(fObjC));

        System.out.println( "Created PIs: " + Abc.Fraig_ManReadInputNum(fAig));

        long fObjAnd, fObjF;
        fObjAnd = Abc.Fraig_NodeAnd(fAig, fObjA, Abc.Fraig_Not(fObjB));
        fObjF = Abc.Fraig_NodeOr(fAig, fObjAnd, fObjC);
        System.out.println( "Created AB with id: " + Abc.Fraig_NodeReadNum(fObjAnd));
        System.out.println( "Created F with id: " + Abc.Fraig_NodeReadNum(fObjF));

        long fObj1, fObj2;
        fObj1 = Abc.Fraig_NodeOr(fAig, Abc.Fraig_NodeAnd(fAig, fObjA, fObjB), Abc.Fraig_NodeAnd(fAig, fObjB, fObjC));
        fObj2 = Abc.Fraig_NodeAnd(fAig, fObjB, Abc.Fraig_NodeOr(fAig, fObjA, fObjC));

        System.out.println( "Created AB+BC with id: " + Abc.Fraig_NodeReadNum(fObj1));
        System.out.println( "Created B(A+C) with id: " + Abc.Fraig_NodeReadNum(fObj2));

        System.out.println( "Pointer F with id: " + (fObjF));
        System.out.println( "Pointer AB+BC with id: " + (fObj1));
        System.out.println( "Pointer B(A+C) with id: " + (fObj2));

        System.out.println( "RegularId F with id: " + Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(fObjF)));
        System.out.println( "RegularId AB+BC with id: " + Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(fObj1)));
        System.out.println( "RegularId B(A+C) with id: " + Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(fObj2)));

        System.out.println( "IsComplement F with id: " + Abc.Fraig_IsComplement(fObjF));
        System.out.println( "IsComplement AB+BC with id: " + Abc.Fraig_IsComplement(fObj1));
        System.out.println( "IsComplement B(A+C) with id: " + Abc.Fraig_IsComplement(fObj2));

        long fObjC1, fObjC2;
        fObjC1 = Abc.Fraig_NodeReadOne(fObjAnd);
        fObjC2 = Abc.Fraig_NodeReadTwo(fObjAnd);

        System.out.println( "Created child1 with id: " + Abc.Fraig_NodeReadNum(fObjC1));
        System.out.println( "Created child2 with id: " + Abc.Fraig_NodeReadNum(fObjC2));

        System.out.println( "RegularId child1 with id: " + Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(fObjC1)));
        System.out.println( "RegularId child2 with id: " + Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(fObjC2)));

        System.out.println( "Is AB+BC == B(A+C) equal? " + Abc.Fraig_NodesAreEqual(fAig, fObj1, fObj2, -1, -1));
        System.out.println( "Is AB+BC == F equal? " + Abc.Fraig_NodesAreEqual(fAig, fObj1, fObjF, -1, -1));

        Abc.Fraig_ManPrintStats(fAig);

        Abc.Fraig_ManFree(fAig);

        System.out.println("Stopping ABC");
        Abc.Abc_Stop();
        System.out.println("Done!");
        throw new RuntimeException("Debug point reached");
    }

    private SolverTrueStatus isAlwaysTrue(long formula, int nBTLimit) {
        if (areEqual(formula, getTrue())) {
            return SolverTrueStatus.True;
        } else if (areEqual(formula, getFalse())) {
            return SolverTrueStatus.NotTrue;
//        } else if (Abc.Fraig_ManCheckClauseUsingSimInfo(Aig.network, formula, Aig.getFalse())) {
//            return SolverTrueStatus.True;
        } else {
            int result = 0;
            result = Abc.Fraig_ManCheckClauseUsingSat(network, formula, getFalse(), nBTLimit);
            Fraig.isSatOperations++;
            switch (result) {
                case 0:
                    Fraig.isSatResult++;
                    return SolverTrueStatus.NotTrue;
                case 1:
                    return SolverTrueStatus.True;
                default:
                    return SolverTrueStatus.Unknown;
            }
        }
    }

    public SatStatus isSat(Long formula, int nBTLimit) {
        switch(isAlwaysTrue(Abc.Fraig_Not(formula), nBTLimit)) {
            case True:
                return SatStatus.Unsat;
            case NotTrue:
                return SatStatus.Sat;
            default:
                return SatStatus.Unknown;
        }
    }

    public Long getTrue() {
        return Abc.Fraig_ManReadConst1(network);
    }

    public Long getFalse() {
        return Abc.Fraig_Not(Abc.Fraig_ManReadConst1(network));
    }

    private Long newAig(Long original) {
        idSet.add(Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(original)));
//        System.out.println("# aig ids: " + idSet.size());
        return original;
    }

    public Long newVar(String name) {
        if (namedNodes.containsKey(name)) {
            return namedNodes.get(name);
        }
        int id = varNames.size();
//        System.out.println("Creating new variable " + name + " with id " + id);
        long result = Abc.Fraig_ManReadIthVar(network, id);
        namedNodes.put(name, result);
        assert(!varNames.containsKey(result));
        varNames.put(result, name);
        return newAig(result);
    }

    public Long not(Long child) {
//        System.out.println(child + " Creating NOT of " + toString(child));
        return newAig(Abc.Fraig_Not(child));
    }

    public Long and(Long childA, Long childB) {
//        System.out.println("Creating AND of " + toString(childA) + " and " + toString(childB));
        return newAig(Abc.Fraig_NodeAnd(network, childA, childB));
    }

    public Long or(Long childA, Long childB) {
//        System.out.println("Creating OR of " + toString(childA) + " and " + toString(childB));
        return newAig(Abc.Fraig_NodeOr(network, childA, childB));
    }

    public Long simplify(Long formula) {
        return formula;
    }

    public SolverGuardType getType(Long formula) {
//        System.out.println("Getting type of " + toString(formula));
        if (Abc.Fraig_NodeIsConst(formula)) {
            if (Abc.Fraig_IsComplement(formula)) {
                return SolverGuardType.FALSE;
            } else {
                return SolverGuardType.TRUE;
            }
        } else {
            if (Abc.Fraig_IsComplement(formula)) {
                return SolverGuardType.NOT;
            } else if (Abc.Fraig_NodeIsVar(formula)) {
                assert (namedNodes.containsKey(toString(formula)));
                return SolverGuardType.VARIABLE;
            } else {
                return SolverGuardType.AND;
            }
        }
    }

    public List<Long> getChildren(Long formula) {
        List<Long> children = new ArrayList<>();
        if (Abc.Fraig_NodeIsConst(formula)) {
        } else {
            if (Abc.Fraig_IsComplement(formula)) {
                children.add(Abc.Fraig_Not(formula));
            } else if (Abc.Fraig_NodeIsVar(formula)) {
            } else {
                children.add(Abc.Fraig_NodeReadOne(formula));
                children.add(Abc.Fraig_NodeReadTwo(formula));
            }
        }
        return children;
    }

    public String toString(Long formula) {
        String result = "";
        if (Abc.Fraig_NodeIsConst(formula)) {
            if (Abc.Fraig_IsComplement(formula)) {
                return "false";
            } else {
                return "true";
            }
        } else {
            if (Abc.Fraig_IsComplement(formula)) {
                result += "(not ";
                result += toString(Abc.Fraig_Not(formula));
                result += ")";
            } else if (Abc.Fraig_NodeIsVar(formula)) {
                assert(varNames.containsKey(formula));
                return varNames.get(formula);
            } else {
                result += "(and ";
                result += toString(Abc.Fraig_NodeReadOne(formula));
                result += " ";
                result += toString(Abc.Fraig_NodeReadTwo(formula));
                result += ")";
            }
        }
        if (result.length() > 80) {
            result = result.substring(0, 80) + "...)";
        }
        return result;
    }

    public String getStats() {
        // Create a stream to hold the output
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        PrintStream ps = new PrintStream(baos);
        // IMPORTANT: Save the old System.out!
        PrintStream old = System.out;
        // Tell Java to use your special stream
        System.setOut(ps);
        // Print some output: goes to your special stream
        Abc.Fraig_ManPrintStats(network);
        // Put things back
        System.out.flush();
        System.setOut(old);
        return baos.toString();
    }

    public int getExprCount() {
        return idSet.size();
    }

    public boolean areEqual(Long left, Long right) {
        if (left == right)
            return true;
        return  (Abc.Fraig_IsComplement(left) == Abc.Fraig_IsComplement(right)) &&
                (Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(left)) == Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(right)));
    }

    public int getHashCode(Long formula) {
        return Abc.Fraig_IsComplement(formula) ? (-1 - Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(formula))) : Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(formula));
    }

}
