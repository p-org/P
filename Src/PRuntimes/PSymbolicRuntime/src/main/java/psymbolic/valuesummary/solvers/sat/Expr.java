package psymbolic.valuesummary.solvers.sat;

import lombok.Getter;
import java.util.*;

public class Expr {
    private static Expr exprTrue = new Expr(ExprType.TRUE, "", Collections.emptyList());
    private static Expr exprFalse = new Expr(ExprType.FALSE, "", Collections.emptyList());
    private static ExprComparator exprComparator = new ExprComparator();
    public static HashMap<Expr, Expr> table = new HashMap<>();
    private static boolean aigMode = false;
    private static boolean simplifyMode = true;

    @Getter
    private final ExprType type;

    @Getter
    private final String name;

    @Getter
    private final List<Expr> children;

    private final int hashCode;

    Expr(ExprType type, String name, List<Expr> children) {
        this.type = type;
        this.name = name;
        this.children = children;
        this.hashCode = Objects.hash(type, name, children);
    }

    private static Expr createExpr(ExprType type, List<Expr> children) {
        Collections.sort(children, exprComparator);
        Expr original = new Expr(type, "", children);
        return simplify(original);
    }

    public static Expr simplify(Expr original) {
        if (!simplifyMode)
            return original;
        if (table.containsKey(original)) {
            return table.get(original);
        }
//        System.out.println("Simplifying formula   :" + original);

        ExprType type = original.getType();
        Expr simplified;

        List<Expr> children = new ArrayList<>();
        for (Expr child: original.getChildren()) {
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
                throw new RuntimeException("Unexpected Expr of type " + type + " : " + original);
        }
//        System.out.println("Original Expr   :" + original);
//        System.out.println("Simplified Expr :" + simplified);
        table.put(original, simplified);
        return simplified;
    }

    private static Expr simplifyNot(List<Expr> children) {
        Expr child = children.get(0);
        switch(child.getType()) {
            case TRUE:
                return exprFalse;
            case FALSE:
                return exprTrue;
            case NOT:
                return child.getChildren().get(0);
            default:
                return new Expr(ExprType.NOT, "", children);
        }
    }

    private static Expr simplifyAnd(List<Expr> children) {
        Queue<Expr> queue = new LinkedList<>(children);
        Set<Expr> newChildren = new HashSet<>();
        while(!queue.isEmpty()) {
            Expr child = queue.remove();
            switch(child.getType()) {
                case TRUE:
                    continue;
                case FALSE:
                    return exprFalse;
                case AND:
                    for (Expr subChild: child.getChildren()) {
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
            List<Expr> sorted = new ArrayList<>();
            for(Expr child: newChildren) {
                sorted.add(child);
            }
            Collections.sort(sorted, exprComparator);
            return new Expr(ExprType.AND, "", sorted);
        }
    }

    private static Expr simplifyOr(List<Expr> children) {
        Queue<Expr> queue = new LinkedList<>(children);
        Set<Expr> newChildren = new HashSet<>();
        while(!queue.isEmpty()) {
            Expr child = queue.remove();
            switch(child.getType()) {
                case TRUE:
                    return exprTrue;
                case FALSE:
                    continue;
                case OR:
                    for (Expr subChild: child.getChildren()) {
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
            List<Expr> sorted = new ArrayList<>();
            for(Expr child: newChildren) {
                if (aigMode) {
                    child = not(child);
                }
                sorted.add(child);
            }
            Collections.sort(sorted, exprComparator);
            if (aigMode) {
                return not(and(sorted));
            } else {
                return new Expr(ExprType.OR, "", sorted);
            }
        }
    }

    public static Expr getTrue() {
        return exprTrue;
    }

    public static Expr getFalse() {
        return exprFalse;
    }

    public static Expr newVar(String name) {
        return new Expr(ExprType.VARIABLE, name, Collections.emptyList());
    }

    public static Expr not(Expr child) {
        return createExpr(ExprType.NOT, Arrays.asList(child));
    }

    public static Expr and(List<Expr> children) {
        return createExpr(ExprType.AND, children);
    }

    public static Expr or(List<Expr> children) {
        return createExpr(ExprType.OR, children);
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
        if (!(o instanceof Expr)) return false;
        Expr that = (Expr) o;
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
