package psym.runtime.values;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.Map;
import psym.runtime.values.exceptions.KeyNotFoundException;

public class PMap extends PCollection {
  // stores the map
  private final Map<PValue<?>, PValue<?>> map;

  public PMap(Map<PValue<?>, PValue<?>> input_map) {
    map = new HashMap<>();
    for (Map.Entry<PValue<?>, PValue<?>> entry : input_map.entrySet()) {
      map.put(PValue.clone(entry.getKey()), PValue.clone(entry.getValue()));
    }
  }

  public PMap(PMap other) {
    map = new HashMap<>();
    for (Map.Entry<PValue<?>, PValue<?>> entry : other.map.entrySet()) {
      map.put(PValue.clone(entry.getKey()), PValue.clone(entry.getValue()));
    }
  }

  public PValue<?> getValue(PValue<?> key) throws KeyNotFoundException {
    if (!map.containsKey(key)) throw new KeyNotFoundException(key, map);
    return map.get(key);
  }

  public void putValue(PValue<?> key, PValue<?> val) {
    map.put(key, val);
  }

  public PSeq getKeys() {
    return new PSeq(new ArrayList<>(map.keySet()));
  }

  @Override
  public PMap clone() {
    return new PMap(map);
  }

  @Override
  public int hashCode() {
    return ComputeHash.getHashCode(map.values()) ^ ComputeHash.getHashCode(map.keySet());
  }

  @Override
  public boolean equals(Object obj) {
    if (obj == this) return true;

    if (!(obj instanceof PMap)) return false;

    PMap other = (PMap) obj;
    if (map.size() != other.map.size()) {
      return false;
    }

    for (PValue<?> key : map.keySet()) {
      if (!other.map.containsKey(key)) {
        return false;
      } else if (!PValue.equals(other.map.get(key), this.map.get(key))) {
        return false;
      }
    }
    return true;
  }

  @Override
  public String toString() {
    StringBuilder sb = new StringBuilder();
    sb.append("(");
    boolean hadElements = false;
    for (PValue<?> key : map.keySet()) {
      if (hadElements) {
        sb.append(", ");
      }
      sb.append(key);
      sb.append("-> ");
      sb.append(map.get(key));
      hadElements = true;
    }
    sb.append(")");
    return sb.toString();
  }

  @Override
  public int size() {
    return map.size();
  }

  @Override
  public boolean contains(PValue<?> item) {
    return map.containsKey(item);
  }
}
