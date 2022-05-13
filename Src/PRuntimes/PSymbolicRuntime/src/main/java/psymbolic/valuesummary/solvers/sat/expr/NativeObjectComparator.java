package psymbolic.valuesummary.solvers.sat.expr;

import java.util.Comparator;
import java.util.List;

class NativeObjectComparator implements Comparator<NativeObject> {
    public int compare(NativeObject a, NativeObject b) {
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

        List<NativeObject> childrenA = a.getChildren();
        List<NativeObject> childrenB = b.getChildren();
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