package psym.valuesummary.util;

import java.util.*;
import psym.valuesummary.Guard;
import psym.valuesummary.PrimitiveVS;

public class ValueSummaryUnionFind extends UnionFind<PrimitiveVS> {

  final Map<PrimitiveVS, Guard> universe = new HashMap<>();

  public ValueSummaryUnionFind(Collection<PrimitiveVS> c) {
    super();
    for (PrimitiveVS elt : c) {
      List<PrimitiveVS> values = new ArrayList<>(new HashSet<>(parents.values()));
      addElement(elt);
      Guard eltUniverse = elt.getUniverse();
      for (int i = 0; i < values.size(); i++) {
        Guard unionUniverse = universe.get(find(values.get(i)));
        if (!eltUniverse.and(unionUniverse).isFalse()) {
          union(elt, values.get(i));
        }
      }
    }
  }

  public Map<Set<PrimitiveVS>, Guard> getLastUniverseMap() {
    Map<Set<PrimitiveVS>, Guard> lastUniverseMap = new HashMap<>();
    for (Set<PrimitiveVS> set : lastDisjointSet) {
      lastUniverseMap.put(set, universe.get(find(set.iterator().next())));
    }
    assert (sanityCheck(lastUniverseMap));
    return lastUniverseMap;
  }

  public boolean sanityCheck(Map<Set<PrimitiveVS>, Guard> universeMap) {
    if (universeMap.size() > 1) {
      for (Map.Entry<Set<PrimitiveVS>, Guard> entry1 : universeMap.entrySet()) {
        Set<PrimitiveVS> val1 = entry1.getKey();
        Guard g1 = entry1.getValue();
        for (Map.Entry<Set<PrimitiveVS>, Guard> entry2 : universeMap.entrySet()) {
          Set<PrimitiveVS> val2 = entry2.getKey();
          Guard g2 = entry2.getValue();
          if (val1 != val2) {
            if (!g1.and(g2).isFalse()) {
              return false;
            }
          }
        }
      }
    }
    return true;
  }

  public void addElement(PrimitiveVS elt) {
    super.addElement(elt);
    universe.put(elt, elt.getUniverse());
  }

  public boolean union(PrimitiveVS e1, PrimitiveVS e2) {
    Guard universe1 = universe.get(find(e1));
    Guard universe2 = universe.get(find(e2));
    boolean res = super.union(e1, e2);
    if (!res) return false;
    universe.put(find(e1), universe1.or(universe2));
    return true;
  }
}
