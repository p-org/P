package pexplicit.values;

import pexplicit.values.exceptions.KeyNotFoundException;

import java.util.ArrayList;
import java.util.Collection;
import java.util.HashMap;
import java.util.Map;

/**
 * Represents the PValue for P map
 */
public class PMap<K extends PValue<K>, V extends PValue<V>> extends PValue<PMap<K, V>> implements PCollection<K> {
    private final Map<K, V> map;

    /**
     * Constructor
     *
     * @param input_map input map to set to
     */
    public PMap(Map<K, V> input_map) {
        map = new HashMap<>();
        for (Map.Entry<K, V> entry : input_map.entrySet()) {
            map.put(PValue.clone(entry.getKey()), PValue.clone(entry.getValue()));
        }
    }

    /**
     * Copy constructor.
     *
     * @param other Value to copy from
     */
    public PMap(PMap<K, V> other) {
        this(other.map);
    }

    /**
     * Empty constructor.
     */
    public PMap() {
        this(new HashMap<>());
    }

    /**
     * Get the mapped value corresponding to a key
     *
     * @param key input key
     * @return value corresponding to the key
     * @throws KeyNotFoundException
     */
    public V get(K key) throws KeyNotFoundException {
        if (!map.containsKey(key)) throw new KeyNotFoundException(key, (Map<PValue<?>, PValue<?>>) map);
        return map.get(key);
    }

    /**
     * Set the mapped value corresponding to a key
     *
     * @param key input key
     * @param val value to set
     */
    public PMap<K, V> put(K key, V val) {
        Map<K, V> newMap = new HashMap<>(map);
        newMap.put(key, val);
        return new PMap(newMap);
    }

    /**
     * Add an entry to the map
     *
     * @param key input key
     * @param val value to set
     */
    public PMap<K, V> add(K key, V val) {
        return put(key, val);
    }

    /**
     * Remove a key from the map
     *
     * @param key input key
     */
    public PMap<K, V> remove(K key) {
        if (!map.containsKey(key)) {
            return this;
        }
        Map<K, V> newMap = new HashMap<>(map);
        newMap.remove(key);
        return new PMap(newMap);
    }

    /**
     * Get the list of keys in the map
     *
     * @return List of keys as a PSeq object
     */
    public PSeq<K> getKeys() {
        return new PSeq(new ArrayList<>(map.keySet()));
    }

    /**
     * Get the number of keys in the map
     *
     * @return Map size
     */
    public PInt size() {
        return new PInt(map.size());
    }

    /**
     * Check if the map contains a given key
     *
     * @param item item to check for.
     * @return true if key is present, false otherwise
     */
    public PBool contains(K item) {
        return new PBool(map.containsKey(item));
    }

    @Override
    public PMap<K, V> clone() {
        return new PMap(map);
    }

    @Override
    public int hashCode() {
        return ComputeHash.getHashCode((Collection<PValue<?>>) map.values()) ^ ComputeHash.getHashCode((Collection<PValue<?>>) map.keySet());
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
            } else if (PValue.notEqual((PValue<?>) other.map.get(key), this.map.get(key))) {
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
        for (K key : map.keySet()) {
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
