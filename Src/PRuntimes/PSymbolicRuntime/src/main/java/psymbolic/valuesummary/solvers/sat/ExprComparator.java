package psymbolic.valuesummary.solvers.sat;

import java.util.Comparator;
import java.util.List;

class ExprComparator implements Comparator<Expr> {
    public int compare(Expr a, Expr b) {
        if (a == b)
            return 0;

        int result = 0;

        result = Integer.compare(a.hashCode(), b.hashCode());
        if (result != 0)
            return result;

        result = a.getType().compareTo(b.getType());
        if (result != 0)
            return result;

        result = a.getName().compareTo(b.getName());
        if (result != 0)
            return result;

        List<Expr> childrenA = a.getChildren();
        List<Expr> childrenB = b.getChildren();
        result = childrenA.size() - childrenB.size();
        if (result != 0)
            return result;

        for (int i = 0; i<childrenA.size(); i++) {
            result = compare(childrenA.get(i), childrenB.get(i));
            if (result != 0)
                return result;
        }
        return 0;
    }
}
