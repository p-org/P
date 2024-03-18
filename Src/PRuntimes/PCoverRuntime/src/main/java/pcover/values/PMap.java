package pcover.values;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.Map;
import pcover.values.exceptions.KeyNotFoundException;

/**
 * Represents the PValue for P map
 */
public class PMap extends PCollection {
  private final Map<PValue<?>, PValue<?>> map;

  /**
   * Constructor
   * @param input_map input map to set to
   */
  public PMap(Map<PValue<?>, PValue<?>> input_map) {
    map = new HashMap<>();
    for (Map.Entry<PValue<?>, PValue<?>> entry : input_map.entrySet()) {
      map.put(PValue.clone(entry.getKey()), PValue.clone(entry.getValue()));
    }
  }

  /**
   * Copy constructor.
   * @param other Value to copy from
   */
  public PMap(PMap other) {
    map = new HashMap<>();
    for (Map.Entry<PValue<?>, PValue<?>> entry : other.map.entrySet()) {
      map.put(PValue.clone(entry.getKey()), PValue.clone(entry.getValue()));
    }
  }

  /**
   * Get the mapped value corresponding to a key
   * @param key input key
   * @return value corresponding to the key
   * @throws KeyNotFoundException
   */
  public PValue<?> getValue(PValue<?> key) throws KeyNotFoundException {
    if (!map.containsKey(key)) throw new KeyNotFoundException(key, map);
    return map.get(key);
  }

  /**
   * Set the mapped value corresponding to a key
   * @param key input key
   * @param val value to set
   */
  public void putValue(PValue<?> key, PValue<?> val) {
    map.put(key, val);
  }

  /**
   * Get the list of keys in the map
   * @return List of keys as a PSeq object
   */
  public PSeq getKeys() {
    return new PSeq(new ArrayList<>(map.keySet()));
  }

  /**
   * Get the number of keys in the map
   * @return Map size
   */
  public int size() {
    return map.size();
  }

  /**
   * Check if the map contains a given key
   * @param item item to check for.
   * @return true if key is present, false otherwise
   */
  public boolean contains(PValue<?> item) {
    return map.containsKey(item);
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
}
