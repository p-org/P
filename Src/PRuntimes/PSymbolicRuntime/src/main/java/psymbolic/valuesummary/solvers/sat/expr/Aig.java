package psymbolic.valuesummary.solvers.sat.expr;

import com.berkeley.abc.Abc;
import psymbolic.valuesummary.solvers.sat.SatExprType;

import java.io.ByteArrayOutputStream;
import java.io.PrintStream;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;

public class Aig implements ExprLib<Long> {
    private static boolean hasStarted = false;
    public static long network;
    private static Long exprTrue;
    private static Long exprFalse;
    private static HashMap<String, Long> namedNodes = new HashMap<String, Long>();
    public static HashSet<Long> idSet = new HashSet<Long>();

    public Aig() {
        reset();
    }

    public void reset() {
        Aig.resetAig();
    }

    public static void resetAig() {
        if (hasStarted) {
            System.out.println("Resetting AIG");
            Abc.Abc_NtkDelete(network);
            System.out.println("Stopping ABC");
            Abc.Abc_Stop();
        }
        hasStarted = true;
        System.out.println("Starting ABC");
        Abc.Abc_Start();
        System.out.println("Setting AIG");
        network = Abc.Abc_NtkAlloc();
//        System.out.println("Creating AIG true");
        exprTrue = Abc.Abc_AigConst1(network);
//        System.out.println("Creating AIG false");
        exprFalse = Abc.Abc_ObjNot(exprTrue);
        namedNodes.clear();
        idSet.clear();
//        debug();
    }

    private static void debug() {
        long pAig = network;

        Abc.Abc_AigPrintNode(exprTrue);
        Abc.Abc_AigPrintNode(exprFalse);

        long pObjA = Abc.Abc_NtkCreatePi(pAig);
        long pObjB = Abc.Abc_NtkCreatePi(pAig);
        long pObjC = Abc.Abc_NtkCreatePi(pAig);
        Abc.Abc_AigPrintNode(pObjA);
        Abc.Abc_AigPrintNode(pObjB);
        Abc.Abc_AigPrintNode(pObjC);

        System.out.println("Created " + Abc.Abc_NtkPiNum(pAig) + " PIs");

        long pObjAnd = Abc.Abc_AigAnd(pAig, pObjA, pObjB);
        long pObjF = Abc.Abc_AigOr(pAig, pObjAnd, Abc.Abc_ObjNot(pObjC));
        Abc.Abc_AigPrintNode(pObjAnd);
        Abc.Abc_AigPrintNode(pObjF);

        System.out.println("Created AB with id: " + Abc.Abc_ObjId(Abc.Abc_ObjRegular(pObjAnd)));
        System.out.println("Created F with id: " + Abc.Abc_ObjId(Abc.Abc_ObjRegular(pObjF)));

        long pObjAnd2;
        pObjAnd2 = Abc.Abc_AigAnd(pAig, exprTrue, exprFalse);
        Abc.Abc_AigPrintNode(pObjAnd2);
        System.out.println("Created t^f with id: " + Abc.Abc_ObjId(Abc.Abc_ObjRegular(pObjAnd2)));

        long pObjO = Abc.Abc_NtkCreatePo(pAig);
        Abc.Abc_ObjAddFanin(pObjO, pObjF);

        Abc.Abc_AigPrintNode(pObjA);
        Abc.Abc_AigPrintNode(pObjB);
        Abc.Abc_AigPrintNode(pObjC);
        Abc.Abc_AigPrintNode(pObjAnd);
        Abc.Abc_AigPrintNode(pObjF);

        Abc.Abc_AigCleanup(pAig);

        if (!Abc.Abc_NtkCheck(pAig)) {
            System.out.println("The AIG construction has failed.");
        } else {
            System.out.println("The AIG construction has succeeded.");
        }

        Abc.Abc_AigPrintNode(pObjA);
        Abc.Abc_AigPrintNode(pObjB);
        Abc.Abc_AigPrintNode(pObjC);
        Abc.Abc_AigPrintNode(pObjAnd);
        Abc.Abc_AigPrintNode(pObjF);

        Abc.Abc_NtkDelete(pAig);

        System.out.println("Stopping ABC");
        Abc.Abc_Stop();
        System.out.println("Done!");
        throw new RuntimeException("Debug point reached");
    }

    public Long getTrue() {
        return exprTrue;
    }

    public Long getFalse() {
        return exprFalse;
    }

    private Long newAig(Long original) {
        idSet.add(Abc.Abc_ObjId(Abc.Abc_ObjRegular(original)));
//        System.out.println("# aig ids: " + idSet.size());
        return original;
    }

    public Long newVar(String name) {
        if (namedNodes.containsKey(name)) {
            return namedNodes.get(name);
        }
//        System.out.println("Creating new variable: " + name);
        long result = Abc.Abc_NtkCreatePi(network);
        Abc.Abc_ObjAssignName(result, name);
        namedNodes.put(name, result);
        return newAig(result);
    }

    public Long not(Long child) {
//        System.out.println("Creating NOT of " + toString(child));
        return newAig(Abc.Abc_ObjNot(child));
    }

    public Long and(Long childA, Long childB) {
//        System.out.println("Creating AND of " + toString(childA) + " and " + toString(childB));
        return newAig(Abc.Abc_AigAnd(network, childA, childB));
    }

    public Long or(Long childA, Long childB) {
//        System.out.println("Creating OR of " + toString(childA) + " and " + toString(childB));
        return newAig(Abc.Abc_AigOr(network, childA, childB));
    }

    public SatExprType getType(Long formula) {
//        System.out.println("Getting type of " + toString(formula));
        if (Abc.Abc_AigNodeIsConst(formula)) {
            if (Abc.Abc_ObjIsComplement(formula)) {
                assert (formula == exprFalse);
                return SatExprType.FALSE;
            } else {
                assert (formula == exprTrue);
                return SatExprType.TRUE;
            }
        } else {
            if (Abc.Abc_ObjIsComplement(formula)) {
                return SatExprType.NOT;
            } else if (Abc.Abc_ObjIsCi(formula)) {
                assert (namedNodes.containsKey(toString(formula)));
                return SatExprType.VARIABLE;
            } else {
                return SatExprType.AND;
            }
        }
    }

    public List<Long> getChildren(Long formula) {
        List<Long> children = new ArrayList<>();
        if (Abc.Abc_AigNodeIsConst(formula)) {
        } else {
            if (Abc.Abc_ObjIsComplement(formula)) {
                children.add(Abc.Abc_ObjRegular(formula));
            } else if (Abc.Abc_ObjIsCi(formula)) {
            } else {
                children.add(Abc.Abc_ObjChild0(formula));
                children.add(Abc.Abc_ObjChild1(formula));
            }
        }
        return children;
    }

    public String toString(Long formula) {
        String result = "";
        if (Abc.Abc_AigNodeIsConst(formula)) {
            if (Abc.Abc_ObjIsComplement(formula)) {
                return "false";
            } else {
                return "true";
            }
        } else {
            if (Abc.Abc_ObjIsComplement(formula)) {
                result += "(not ";
                result += toString(Abc.Abc_ObjRegular(formula));
                result += ")";
            } else if (Abc.Abc_ObjIsCi(formula)) {
                return Abc.Abc_ObjName(formula);
            } else {
                result += "(and ";
                result += toString(Abc.Abc_ObjChild0(formula));
                result += " ";
                result += toString(Abc.Abc_ObjChild1(formula));
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
        Abc.Fraig_ManPrintStats(Fraig.network);
        // Put things back
        System.out.flush();
        System.setOut(old);
        return baos.toString();
    }

    public int getExprCount() {
        return idSet.size();
    }

    public boolean areEqual(Long left, Long right) {
//        return left == right;
        return  (Abc.Abc_ObjIsComplement(left) == Abc.Abc_ObjIsComplement(right)) &&
                (Abc.Abc_ObjId(Abc.Abc_ObjRegular(left)) == Abc.Abc_ObjId(Abc.Abc_ObjRegular(right)));
    }

    public int getHashCode(Long formula) {
        return Long.hashCode(formula);
    }

}