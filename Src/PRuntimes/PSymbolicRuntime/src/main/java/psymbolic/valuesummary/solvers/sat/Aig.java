package psymbolic.valuesummary.solvers.sat;
import com.berkeley.abc.Abc;
import psymbolic.runtime.statistics.SolverStats;
import psymbolic.valuesummary.solvers.SolverTrueStatus;

import java.io.ByteArrayOutputStream;
import java.io.PrintStream;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;

public class Aig {
    private static boolean hasStarted = false;
    public static long network;
    private static long params;
    private static long exprTrue;
    private static long exprFalse;
    private static HashMap<String, Long> namedNodes = new HashMap<String, Long>();
    private static HashMap<Long, String> varNames = new HashMap<Long, String>();
    public static HashSet<Integer> idSet = new HashSet<Integer>();
    public static int isSatOperations = 0;
    public static int isSatResult = 0;
    public static int nBTLimit = 50;

    private static void debug() {
        long fAig = network;

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

    public static void resetAig() {
        if (hasStarted) {
            System.out.println("Resetting AIG");
            Abc.Fraig_ManFree(network);
            System.out.println("Stopping ABC");
            Abc.Abc_Stop();
        }
        hasStarted = true;
        System.out.println("Starting ABC");
        Abc.Abc_Start();

        System.out.println("Setting AIG Parameters");
        params = Abc.Fraig_ParamsGetDefault();
        Abc.Fraig_ParamsSet_nPatsRand(params, 1024);
        Abc.Fraig_ParamsSet_nPatsDyna(params, 1024);
        Abc.Fraig_ParamsSet_nBTLimit(params, 50);
        Abc.Fraig_ParamsSet_nSeconds(params, 1);
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

        System.out.println("Creating AIG Network");
        network = Abc.Fraig_ManCreate(params);
//        network = Abc.Fraig_ManCreate(-1);

//        System.out.println("Creating AIG true");
        exprTrue = Abc.Fraig_ManReadConst1(network);
//        System.out.println("Creating AIG false");
        exprFalse = Abc.Fraig_Not(exprTrue);
        namedNodes.clear();
//        debug();
    }

    private static SolverTrueStatus isAlwaysTrue(long formula, int nBTLimit) {
        if (formula == Aig.getTrue()) {
            return SolverTrueStatus.True;
        } else if (formula == Aig.getFalse()) {
            return SolverTrueStatus.NotTrue;
//        } else if (Abc.Fraig_ManCheckClauseUsingSimInfo(Aig.network, formula, Aig.getFalse())) {
//            return SolverTrueStatus.True;
        } else {
            int result = Abc.Fraig_ManCheckClauseUsingSat(Aig.network, formula, Aig.getFalse(), nBTLimit);
            switch (result) {
                case 0:
                    return SolverTrueStatus.NotTrue;
                case 1:
                    return SolverTrueStatus.True;
                default:
                    return SolverTrueStatus.Unknown;
            }
        }
    }

    public static SatStatus isSat(long formula, int nBTLimit) {
        Aig.isSatOperations++;
        switch(isAlwaysTrue(Abc.Fraig_Not(formula), nBTLimit)) {
            case True:
                Aig.isSatResult++;
                return SatStatus.Unsat;
            case NotTrue:
                return SatStatus.Sat;
            default:
                return SatStatus.Unknown;
        }
    }

    public static long getTrue() {
        return exprTrue;
    }

    public static long getFalse() {
        return exprFalse;
    }

    private static long newAig(long original) {
        idSet.add(Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(original)));
//        System.out.println("# aig ids: " + idSet.size());
        return original;
    }

    public static long newVar(String name) {
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

    public static long not(long child) {
//        System.out.println(child + " Creating NOT of " + toString(child));
        return newAig(Abc.Fraig_Not(child));
    }

    public static long and(long childA, long childB) {
//        System.out.println("Creating AND of " + toString(childA) + " and " + toString(childB));
        return newAig(Abc.Fraig_NodeAnd(network, childA, childB));
    }

    public static long or(long childA, long childB) {
//        System.out.println("Creating OR of " + toString(childA) + " and " + toString(childB));
        return newAig(Abc.Fraig_NodeOr(network, childA, childB));
    }

    public static ExprType getType(long formula) {
//        System.out.println("Getting type of " + toString(formula));
        if (Abc.Fraig_NodeIsConst(formula)) {
            if (Abc.Fraig_IsComplement(formula)) {
                assert (formula == exprFalse);
                return ExprType.FALSE;
            } else {
                assert (formula == exprTrue);
                return ExprType.TRUE;
            }
        } else {
            if (Abc.Fraig_IsComplement(formula)) {
                return ExprType.NOT;
            } else if (Abc.Fraig_NodeIsVar(formula)) {
                assert (namedNodes.containsKey(toString(formula)));
                return ExprType.VARIABLE;
            } else {
                return ExprType.AND;
            }
        }
    }

    public static List<Long> getChildren(long formula) {
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

    public static String toString(long formula) {
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

    public static String getStats() {
        // Create a stream to hold the output
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        PrintStream ps = new PrintStream(baos);
        // IMPORTANT: Save the old System.out!
        PrintStream old = System.out;
        // Tell Java to use your special stream
        System.setOut(ps);
        // Print some output: goes to your special stream
        Abc.Fraig_ManPrintStats(Aig.network);
        // Put things back
        System.out.flush();
        System.setOut(old);
        return baos.toString();
    }

    public static boolean areEqual(long left, long right) {
//        return left == right;
        return  (Abc.Fraig_IsComplement(left) == Abc.Fraig_IsComplement(right)) &&
                (Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(left)) == Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(right)));
    }

    public static int getHashCode(long formula) {
        return (int)formula;
//        return Long.hashCode(Abc.Abc_ObjId(formula));
    }

}
