package psymbolic.valuesummary.solvers.sat;
import com.berkeley.abc.Abc;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;

public class Aig {
    private static boolean hasStarted = false;
    public static long network;
    private static long exprTrue;
    private static long exprFalse;
    private static HashMap<String, Long> namedNodes = new HashMap<String, Long>();
    private static HashMap<Long, String> varNames = new HashMap<Long, String>();
    private static HashSet<Integer> idSet = new HashSet<Integer>();

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
        System.out.println("Setting AIG");
        network = Abc.Fraig_ManCreate();
//        System.out.println("Creating AIG true");
        exprTrue = Abc.Fraig_ManReadConst1(network);
//        System.out.println("Creating AIG false");
        exprFalse = Abc.Fraig_Not(exprTrue);
        namedNodes.clear();
//        debug();
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
                children.add(Abc.Fraig_Regular(formula));
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
                result += toString(Abc.Fraig_Regular(formula));
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

    public static boolean areEqual(long left, long right) {
        return  (Abc.Fraig_IsComplement(left) == Abc.Fraig_IsComplement(right)) &&
                (Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(left)) == Abc.Fraig_NodeReadNum(Abc.Fraig_Regular(right)));
    }

    public static int getHashCode(long formula) {
        return (int)formula;
//        return Long.hashCode(Abc.Abc_ObjId(formula));
    }

}
