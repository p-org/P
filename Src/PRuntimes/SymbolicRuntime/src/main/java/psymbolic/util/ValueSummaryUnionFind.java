package psymbolic.util;

import psymbolic.valuesummary.PrimitiveVS;
import psymbolic.valuesummary.bdd.Bdd;

import java.util.*;

public class ValueSummaryUnionFind extends UnionFind<PrimitiveVS> {

    Map<PrimitiveVS, Bdd> universe = new HashMap<>();

    public ValueSummaryUnionFind(Collection<PrimitiveVS> c) {
        super();
        for (PrimitiveVS elt : c) {
            List<PrimitiveVS> values = new ArrayList<>(new HashSet<>(parents.values()));
            addElement(elt);
            Bdd eltUniverse = elt.getUniverse();
            for (int i = 0; i < values.size(); i ++) {
                Bdd unionUniverse = universe.get(find(values.get(i)));
                if (!eltUniverse.and(unionUniverse).isConstFalse()) {
                    union(elt, values.get(i));
                    if (eltUniverse.implies(unionUniverse).isConstTrue()) {
                        break;
                    }
                }
            }
        }
    }

    public Map<Set<PrimitiveVS>, Bdd> getLastUniverseMap() {
        Map<Set<PrimitiveVS>, Bdd> lastUniverseMap = new HashMap<>();
        for (Set<PrimitiveVS> set : lastDisjointSet) {
            lastUniverseMap.put(set, universe.get(find(set.iterator().next())));
        }
        return lastUniverseMap;
    }

    public void addElement(PrimitiveVS elt) {
        super.addElement(elt);
        universe.put(elt, elt.getUniverse());
    }

    public boolean union(PrimitiveVS e1, PrimitiveVS e2) {
        Bdd universe1 = universe.get(find(e1));
        Bdd universe2 = universe.get(find(e2));
        boolean res = super.union(e1, e2);
        if (!res) return false;
        universe.put(find(e1), universe1.or(universe2));
        return res;
    }
}
