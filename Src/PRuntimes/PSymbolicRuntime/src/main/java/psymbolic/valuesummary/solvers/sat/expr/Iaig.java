package psymbolic.valuesummary.solvers.sat.expr;
import com.berkeley.abc.Abc;
import psymbolic.valuesummary.solvers.SolverEngine;
import psymbolic.valuesummary.solvers.SolverTrueStatus;
import psymbolic.valuesummary.solvers.SolverGuardType;
import psymbolic.valuesummary.solvers.sat.SatStatus;

import java.io.ByteArrayOutputStream;
import java.io.PrintStream;
import java.util.*;

public class Iaig implements ExprLib<Long> {
    private boolean hasStarted = false;
    private long network2;
    private long network;
    private long params;
    private HashMap<String, Long> namedNodes = new HashMap<String, Long>();
    private HashMap<Long, String> varNames = new HashMap<Long, String>();
    public HashSet<Integer> idSet = new HashSet<Integer>();
    private HashSet<Long> nodeSet = new HashSet<Long>();
    private HashSet<Long> tmpSet = new HashSet<Long>();

    public Iaig() {
        reset();
    }

    public void reset() {
        resetAig();
    }

    public void resetAig() {
        if (hasStarted) {
            System.out.println("Resetting IAIG");
            Abc.Ivy_ManStop(network);
        }
        hasStarted = true;

        System.out.println("Setting IAIG Parameters");
        params = Abc.Ivy_FraigParamsDefault();

        System.out.println("Creating IAIG Network");
        network = Abc.Ivy_ManStart();

        namedNodes.clear();
        varNames.clear();
        idSet.clear();
        nodeSet.clear();
        tmpSet.clear();

//        debug();
    }

    private void debug() {
        long exprTrue = getTrue();
        long exprFalse = getFalse();
        long fAig = network;

        System.out.println( "Created true with id: " + Abc.Ivy_ObjId(exprTrue));
        System.out.println( "Created false with id: " + Abc.Ivy_ObjId(exprFalse));

        long fObjAnd2;
        fObjAnd2 = Abc.Ivy_And(fAig, exprTrue, exprFalse);
        System.out.println( "Created t^f with id: " + Abc.Ivy_ObjId(fObjAnd2));

        long fObjA, fObjB, fObjC;
        fObjA = Abc.Ivy_ObjCreatePi(fAig);
        fObjB = Abc.Ivy_ObjCreatePi(fAig);
        fObjC = Abc.Ivy_ObjCreatePi(fAig);

        System.out.println( "Created PI with id: " + Abc.Ivy_ObjId(fObjA));
        System.out.println( "Created PI with id: " + Abc.Ivy_ObjId(fObjB));
        System.out.println( "Created PI with id: " + Abc.Ivy_ObjId(fObjC));

        System.out.println( "Created PIs: " + Abc.Ivy_ManPiNum(fAig));

        long fObjAnd, fObjF;
        fObjAnd = Abc.Ivy_And(fAig, fObjA, Abc.Ivy_Not(fObjB));
        fObjF = Abc.Ivy_Or(fAig, fObjAnd, fObjC);
        System.out.println( "Created AB with id: " + Abc.Ivy_ObjId(fObjAnd));
        System.out.println( "Created F with id: " + Abc.Ivy_ObjId(fObjF));

        long fObjC1, fObjC2;
        fObjC1 = Abc.Ivy_ObjChild0(fObjAnd);
        fObjC2 = Abc.Ivy_ObjChild1(fObjAnd);

        System.out.println( "Created child1 with id: " + Abc.Ivy_ObjId(fObjC1));
        System.out.println( "Created child2 with id: " + Abc.Ivy_ObjId(fObjC2));

        System.out.println( "RegularId child1 with id: " + Abc.Ivy_ObjId(Abc.Ivy_Regular(fObjC1)));
        System.out.println( "RegularId child2 with id: " + Abc.Ivy_ObjId(Abc.Ivy_Regular(fObjC2)));

        long fObj1, fObj2;
        fObj1 = Abc.Ivy_Or(fAig, Abc.Ivy_And(fAig, fObjA, fObjB), Abc.Ivy_And(fAig, fObjB, fObjC));
        fObj2 = Abc.Ivy_And(fAig, fObjB, Abc.Ivy_Or(fAig, fObjA, fObjC));

        System.out.println( "Created AB+BC with id: " + Abc.Ivy_ObjId(fObj1));
        System.out.println( "Created B(A+C) with id: " + Abc.Ivy_ObjId(fObj2));

        System.out.println( "Pointer F with id: " + (fObjF));
        System.out.println( "Pointer AB+BC with id: " + (fObj1));
        System.out.println( "Pointer B(A+C) with id: " + (fObj2));

        System.out.println( "RegularId F with id: " + Abc.Ivy_ObjId(Abc.Ivy_Regular(fObjF)));
        System.out.println( "RegularId AB+BC with id: " + Abc.Ivy_ObjId(Abc.Ivy_Regular(fObj1)));
        System.out.println( "RegularId B(A+C) with id: " + Abc.Ivy_ObjId(Abc.Ivy_Regular(fObj2)));

        System.out.println( "IsComplement F with id: " + Abc.Ivy_IsComplement(fObjF));
        System.out.println( "IsComplement AB+BC with id: " + Abc.Ivy_IsComplement(fObj1));
        System.out.println( "IsComplement B(A+C) with id: " + Abc.Ivy_IsComplement(fObj2));

        fObj1 = exprTrue;
        fObj2 = Abc.Ivy_Not(exprTrue);
        System.out.println( "Pointer fObj1 with id: " + (fObj1));
        System.out.println( "Pointer fObj2 with id: " + (fObj2));
        System.out.println( "RegularId fObj1 with id: " + Abc.Ivy_ObjId(Abc.Ivy_Regular(fObj1)));
        System.out.println( "RegularId fObj2 with id: " + Abc.Ivy_ObjId(Abc.Ivy_Regular(fObj2)));
        System.out.println( "IsComplement fObj1 with id: " + Abc.Ivy_IsComplement(fObj1));
        System.out.println( "IsComplement fObj2 with id: " + Abc.Ivy_IsComplement(fObj2));

        long fAigNew = Abc.Ivy_FraigPerform(fAig, params);

        fObj1 = Abc.Ivy_ObjEquiv(fObj1);
        fObj2 = Abc.Ivy_ObjEquiv(fObj2);
        System.out.println( "Pointer fObj1 with id: " + (fObj1));
        System.out.println( "Pointer fObj2 with id: " + (fObj2));
        System.out.println( "RegularId fObj1 with id: " + Abc.Ivy_ObjId(Abc.Ivy_Regular(fObj1)));
        System.out.println( "RegularId fObj2 with id: " + Abc.Ivy_ObjId(Abc.Ivy_Regular(fObj2)));
        System.out.println( "IsComplement fObj1 with id: " + Abc.Ivy_IsComplement(fObj1));
        System.out.println( "IsComplement fObj2 with id: " + Abc.Ivy_IsComplement(fObj2));

        Abc.Ivy_ManPrintStats(fAig);

        Abc.Ivy_ManStop(fAig);

        System.out.println("Stopping ABC");
        Abc.Abc_Stop();
        System.out.println("Done!");
        throw new RuntimeException("Debug point reached");
    }

    public Long getTrue() {
        return newAig(Abc.Ivy_ManConst1(network), false);
    }

    public Long getFalse() {
        return newAig(Abc.Ivy_Not(Abc.Ivy_ManConst1(network)), false);
    }

    private Long newAig(Long original, boolean simplify) {
        nodeSet.add(original);
        idSet.add(Abc.Ivy_ObjId(Abc.Ivy_Regular(original)));
        return original;
    }

    public Long newVar(String name) {
        if (namedNodes.containsKey(name)) {
            return namedNodes.get(name);
        }
//        System.out.println("Creating new variable " + name);
        long result = Abc.Ivy_ObjCreatePi(network);
        namedNodes.put(name, result);
        assert(!varNames.containsKey(result));
        varNames.put(result, name);
        return newAig(result, false);
    }

    private void checkInputs(List<Long> inputs) {
        for (Long input : inputs) {
            assert(nodeSet.contains(input));
        }
    }

    public Long not(Long child) {
        checkInputs(Arrays.asList(child));
//        System.out.println(child + " Creating NOT of " + toString(child));
        return newAig(Abc.Ivy_Not(child), false);
    }

    public Long and(Long childA, Long childB) {
        checkInputs(Arrays.asList(childA, childB));
//        System.out.println("Creating AND of " + toString(childA) + " and " + toString(childB));
        return newAig(Abc.Ivy_And(network, childA, childB), true);
    }

    public Long or(Long childA, Long childB) {
        checkInputs(Arrays.asList(childA, childB));
//        System.out.println("Creating OR of " + toString(childA) + " and " + toString(childB));
        return newAig(Abc.Ivy_Or(network, childA, childB), true);
    }

    public Long simplify(Long formula) {
        checkInputs(Arrays.asList(formula));
//        System.out.println("\toriginal: ");
//        System.out.println("\t\t" + formula);
//        System.out.println("\t\t" + getHashCode(formula));
//        System.out.println("\t\t" + Abc.Ivy_ObjId(Abc.Ivy_Regular(formula)));
//        System.out.println("\t\t" + toString(formula));

        long simplifiedFormula = Abc.Ivy_ObjEquiv(formula);
//        System.out.println("\tsimplified: ");
//        System.out.println("\t\t: " + simplifiedFormula);
        assert(simplifiedFormula != 0);

        Long simplified = newAig(simplifiedFormula, false);
        if (varNames.containsKey(formula)) {
            String name = varNames.get(formula);
            namedNodes.remove(name);
//            varNames.remove(formula);
            namedNodes.put(name, simplified);
            varNames.put(simplified, name);
        }
//        System.out.println("\t\t" + getHashCode(simplifiedFormula));
//        System.out.println("\t\t" + Abc.Ivy_ObjId(Abc.Ivy_Regular(simplified)));
//        System.out.println("\t\t" + toString(simplified));
        return simplified;
    }

    public void startSimplify() {
        idSet.clear();
        tmpSet.addAll(nodeSet);
        network2 = Abc.Ivy_FraigPerform(network, params);
    }

    public void stopSimplify() {
        nodeSet.removeAll(tmpSet);
        tmpSet.clear();
        Abc.Ivy_ManStop(network);
        network = network2;
    }

    public SolverGuardType getType(Long formula) {
        if (Abc.Ivy_ObjIsConst1(Abc.Ivy_Regular(formula))) {
            if (Abc.Ivy_IsComplement(formula)) {
                return SolverGuardType.FALSE;
            } else {
                return SolverGuardType.TRUE;
            }
        } else {
            if (Abc.Ivy_IsComplement(formula)) {
                return SolverGuardType.NOT;
            } else if (Abc.Ivy_ObjIsPi(formula)) {
                assert (namedNodes.containsKey(toString(formula)));
                return SolverGuardType.VARIABLE;
            } else {
                return SolverGuardType.AND;
            }
        }
    }

    public List<Long> getChildren(Long formula) {
        List<Long> children = new ArrayList<>();
        if (Abc.Ivy_ObjIsConst1(Abc.Ivy_Regular(formula))) {
        } else {
            if (Abc.Ivy_IsComplement(formula)) {
                children.add(Abc.Ivy_Not(formula));
            } else if (Abc.Ivy_ObjIsPi(formula)) {
            } else {
                children.add(Abc.Ivy_ObjChild0(formula));
                children.add(Abc.Ivy_ObjChild1(formula));
            }
        }
        return children;
    }

    public String toString(Long formula) {
        String result = "";
        if (Abc.Ivy_ObjIsConst1(Abc.Ivy_Regular(formula))) {
            if (Abc.Ivy_IsComplement(formula)) {
                return "false";
            } else {
                return "true";
            }
        } else {
            if (Abc.Ivy_IsComplement(formula)) {
                result += "(not ";
                result += toString(Abc.Ivy_Not(formula));
                result += ")";
            } else if (Abc.Ivy_ObjIsPi(formula)) {
                assert(varNames.containsKey(formula));
                return varNames.get(formula);
            } else {
                result += "(and ";
                result += toString(Abc.Ivy_ObjChild0(formula));
                result += " ";
                result += toString(Abc.Ivy_ObjChild1(formula));
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
        Abc.Ivy_ManPrintStats(network);
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
        return  (Abc.Ivy_IsComplement(left) == Abc.Ivy_IsComplement(right)) &&
                (Abc.Ivy_ObjId(Abc.Ivy_Regular(left)) == Abc.Ivy_ObjId(Abc.Ivy_Regular(right)));
    }

    public int getHashCode(Long formula) {
        if (!nodeSet.contains(formula)) {
            System.out.println("Missing formula when hashing: " + formula);
        }
        return Abc.Ivy_IsComplement(formula) ? (-1 - Abc.Ivy_ObjId(Abc.Ivy_Regular(formula))) : Abc.Ivy_ObjId(Abc.Ivy_Regular(formula)) ;
//        return Long.hashCode(formula);
    }

}
