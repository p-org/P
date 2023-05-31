package psym.runtime.scheduler.symmetry;

import java.util.*;

public class SymmetrySet {
    SortedSet<Object> elements;

    public SymmetrySet() {
        elements = new TreeSet<>();
    }

    public void add(Object element) {
        elements.add(element);
    }

    public void remove(Object element) {
        elements.remove(element);
    }

    public boolean contains(Object element) {
        return elements.contains(element);
    }

    public Object getRepresentative() {
        assert (!elements.isEmpty());
        return elements.first();
    }

    @Override
    public String toString() {
        StringBuilder out = new StringBuilder();
        Iterator itr = elements.iterator();
        out.append("{ ");
        while (itr.hasNext()) {
            out.append(itr.next());
            if (itr.hasNext()) {
                out.append(", ");
            }
        }
        out.append("}");
        return out.toString();
    }

}
