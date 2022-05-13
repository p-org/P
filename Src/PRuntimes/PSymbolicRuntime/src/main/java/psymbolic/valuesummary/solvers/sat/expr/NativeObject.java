package psymbolic.valuesummary.solvers.sat.expr;

import lombok.Getter;
import psymbolic.valuesummary.solvers.SolverGuardType;

import java.util.*;

public class NativeObject {
    private static NativeObject exprTrue = new NativeObject(SolverGuardType.TRUE, "", Collections.emptyList());
    private static NativeObject exprFalse = new NativeObject(SolverGuardType.FALSE, "", Collections.emptyList());
    private static NativeObjectComparator exprComparator = new NativeObjectComparator();
    public static HashMap<NativeObject, NativeObject> simplifyTable = new HashMap<>();
    private static boolean aigMode = false;

    @Getter
    private final SolverGuardType type;

    @Getter
    private final String name;

    @Getter
    private final List<NativeObject> children;

    private final int hashCode;

    NativeObject(SolverGuardType type, String name, List<NativeObject> children) {
        this.type = type;
        this.name = name;
        this.children = children;
        this.hashCode = Objects.hash(type, name, children);
    }

    public static int getExprCount() {
        return simplifyTable.size();
    }

    private static NativeObject createExpr(SolverGuardType type, List<NativeObject> children) {
        Collections.sort(children, exprComparator);
        NativeObject original = new NativeObject(type, "", children);
        return simplify(original);
    }

    public static NativeObject simplify(NativeObject original) {
        if (simplifyTable.containsKey(original)) {
            return simplifyTable.get(original);
        }
//        System.out.println("Simplifying formula   :" + original);

        SolverGuardType type = original.getType();
        NativeObject simplified;

        List<NativeObject> children = new ArrayList<>();
        for (NativeObject child: original.getChildren()) {
            children.add(simplify(child));
        }

        switch(type) {
            case TRUE:
            case FALSE:
            case VARIABLE:
                simplified = original;
                break;
            case NOT:
                simplified = simplifyNot(children);
                break;
            case AND:
                simplified = simplifyAnd(children);
                break;
            case OR:
                simplified = simplifyOr(children);
                break;
            default:
                throw new RuntimeException("Unexpected NativeObject of type " + type + " : " + original);
        }
//        System.out.println("Original NativeObject   :" + original);
//        System.out.println("Simplified NativeObject :" + simplified);
        simplifyTable.put(original, simplified);
        return simplified;
    }

    private static NativeObject simplifyNot(List<NativeObject> children) {
        NativeObject child = children.get(0);
        switch(child.getType()) {
            case TRUE:
                return exprFalse;
            case FALSE:
                return exprTrue;
            case NOT:
                return child.getChildren().get(0);
            default:
                return new NativeObject(SolverGuardType.NOT, "", children);
        }
    }

    private static NativeObject simplifyAnd(List<NativeObject> children) {
        Queue<NativeObject> queue = new LinkedList<>(children);
        Set<NativeObject> newChildren = new HashSet<>();
        while(!queue.isEmpty()) {
            NativeObject child = queue.remove();
            switch(child.getType()) {
                case TRUE:
                    continue;
                case FALSE:
                    return exprFalse;
                case AND:
                    for (NativeObject subChild: child.getChildren()) {
                        queue.add(subChild);
                    }
                    break;
                default:
                    if (newChildren.contains(not(child))) {
                        return exprFalse;
                    } else {
                        newChildren.add(child);
                    }
            }
        }
        int size = newChildren.size();
        if (size == 0) {
            return exprTrue;
        } else if (size == 1) {
            return newChildren.iterator().next();
        } else {
            List<NativeObject> sorted = new ArrayList<>();
            for(NativeObject child: newChildren) {
                sorted.add(child);
            }
            Collections.sort(sorted, exprComparator);
            return new NativeObject(SolverGuardType.AND, "", sorted);
        }
    }

    private static NativeObject simplifyOr(List<NativeObject> children) {
        Queue<NativeObject> queue = new LinkedList<>(children);
        Set<NativeObject> newChildren = new HashSet<>();
        while(!queue.isEmpty()) {
            NativeObject child = queue.remove();
            switch(child.getType()) {
                case TRUE:
                    return exprTrue;
                case FALSE:
                    continue;
                case OR:
                    for (NativeObject subChild: child.getChildren()) {
                        queue.add(subChild);
                    }
                    break;
                default:
                    if (newChildren.contains(not(child))) {
                        return exprTrue;
                    } else {
                        newChildren.add(child);
                    }
            }
        }
        int size = newChildren.size();
        if (size == 0) {
            return exprFalse;
        } else if (size == 1) {
            return newChildren.iterator().next();
        } else {
            List<NativeObject> sorted = new ArrayList<>();
            for(NativeObject child: newChildren) {
                if (aigMode) {
                    child = not(child);
                }
                sorted.add(child);
            }
            Collections.sort(sorted, exprComparator);
            if (aigMode) {
                return not(and(sorted));
            } else {
                return new NativeObject(SolverGuardType.OR, "", sorted);
            }
        }
    }

    public static NativeObject getTrue() {
        return exprTrue;
    }

    public static NativeObject getFalse() {
        return exprFalse;
    }

    public static NativeObject newVar(String name) {
        return new NativeObject(SolverGuardType.VARIABLE, name, Collections.emptyList());
    }

    public static NativeObject not(NativeObject child) {
        return createExpr(SolverGuardType.NOT, Arrays.asList(child));
    }

    public static NativeObject and(List<NativeObject> children) {
        return createExpr(SolverGuardType.AND, children);
    }

    public static NativeObject or(List<NativeObject> children) {
        return createExpr(SolverGuardType.OR, children);
    }

    @Override
    public String toString() {
        String result = "";
        switch(this.type) {
            case TRUE:
                return "true";
            case FALSE:
                return "false";
            case VARIABLE:
                return name;
            case NOT:
                result += "(not ";
                break;
            case AND:
                result += "(and ";
                break;
            case OR:
                result += "(or ";
                break;
        }
        for (int i=0; i< children.size(); i++) {
            result += children.get(i).toString();
            if (i != children.size() - 1) {
                result += " ";
                if (result.length() > 80) {
                    return result.substring(0, 80) + "...)";
                }
            }
        }
        result += ")";
        return result;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (!(o instanceof NativeObject)) return false;
        NativeObject that = (NativeObject) o;
        return this.type.equals(that.type) &&
                this.name.equals(that.name) &&
                this.children.hashCode() == that.children.hashCode() &&
                this.children.equals(that.children);
    }

    @Override
    public int hashCode() {
        return this.hashCode;
    }
}
