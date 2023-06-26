package psym.valuesummary.util;

import java.io.Serializable;
import java.util.*;

public class UnionFind<T> implements Serializable {
  final Map<T, T> parents = new HashMap<>();
  final Map<T, Integer> rank = new HashMap<>();
  Collection<Set<T>> lastDisjointSet = null;

  public UnionFind() {}

  public UnionFind(Collection<T> c) {
    c.forEach(this::addElement);
  }

  public void addElement(T elt) {
    parents.put(elt, elt);
    rank.put(elt, 0);
  }

  public T find(T elt) {
    while (!elt.equals(parents.get(elt))) {
      T parent = parents.get(parents.get(elt));
      parents.put(elt, parent);
      elt = parent;
    }
    return elt;
  }

  public boolean union(T e1, T e2) {
    T root1 = find(e1);
    T root2 = find(e2);
    if (root1.equals(root2)) return false;
    Integer rank1 = rank.get(root1);
    Integer rank2 = rank.get(root2);
    if (rank1 > rank2) {
      parents.put(root2, root1);
    } else {
      parents.put(root1, root2);
      if (rank1.equals(rank2)) {
        rank.put(root2, rank2 + 1);
      }
    }
    return true;
  }

  public Collection<Set<T>> getDisjointSets() {
    if (lastDisjointSet == null) {
      Map<T, Set<T>> collectSets = new HashMap<>();
      for (Map.Entry<T, T> entry : parents.entrySet()) {
        T root = find(entry.getValue());
        collectSets.putIfAbsent(root, new HashSet<>());
        collectSets.get(root).add(entry.getKey());
      }
      lastDisjointSet = collectSets.values();
    }
    return lastDisjointSet;
  }
}
