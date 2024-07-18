package pexplicit.values;

import pexplicit.utils.misc.Assert;
import pexplicit.values.exceptions.KeyNotFoundException;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Represents the PValue for P map
 */
public class PMap extends PValue<PMap> implements PCollection {
    private final Map<PValue<?>, PValue<?>> map;

    /**
     * Constructor
     *
     * @param input_map input map to set to
     */
    public PMap(Map<PValue<?>, PValue<?>> input_map) {
        map = new HashMap<>();
        for (Map.Entry<PValue<?>, PValue<?>> entry : input_map.entrySet()) {
            map.put(PValue.clone(entry.getKey()), PValue.clone(entry.getValue()));
        }
        initialize();
    }

    /**
     * Copy constructor.
     *
     * @param other Value to copy from
     */
    public PMap(PMap other) {
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
    public PValue<?> get(PValue<?> key) throws KeyNotFoundException {
        if (!map.containsKey(key)) throw new KeyNotFoundException(key, map);
        return map.get(key);
    }

    /**
     * Get the mapped value corresponding to a key or a given default value if key doesn't exists
     *
     * @param key input key
     * @param def input default value
     * @return value corresponding to the key or default value if key does not exists
     */
    public PValue<?> getOrDefault(PValue<?> key, PValue<?> def) {
        try {
            return get(key);
        } catch (KeyNotFoundException e) {
            return def;
        }
    }

    /**
     * Set the mapped value corresponding to a key
     *
     * @param key input key
     * @param val value to set
     */
    public PMap put(PValue<?> key, PValue<?> val) {
        Map<PValue<?>, PValue<?>> newMap = new HashMap<>(map);
        newMap.put(key, val);
        return new PMap(newMap);
    }

    /**
     * Add an entry to the map
     *
     * @param key input key
     * @param val value to set
     */
    public PMap add(PValue<?> key, PValue<?> val) {
        if (map.containsKey(key)) {
            Assert.fromModel(
                    false,
                    String.format(
                            "ArgumentException: An item with the same key has already been added. Key: %s, Value: %s",
                            key, map.get(key)));
        }
        return put(key, val);
    }

    /**
     * Remove a key from the map
     *
     * @param key input key
     */
    public PMap remove(PValue<?> key) {
        if (!map.containsKey(key)) {
            return this;
        }
        Map<PValue<?>, PValue<?>> newMap = new HashMap<>(map);
        newMap.remove(key);
        return new PMap(newMap);
    }

    /**
     * Get the list of keys in the map
     *
     * @return List of keys as a PSeq object
     */
    public PSeq getKeys() {
        return new PSeq(new ArrayList<>(map.keySet()));
    }

    /**
     * Get the list of values in the map
     *
     * @return List of values as a PSeq object
     */
    public PSeq getValues() {
        return new PSeq(new ArrayList<>(map.values()));
    }

    /**
     * Convert the PMap to a List of PValues representing map keys.
     *
     * @return List of PValues corresponding to the PMap keys.
     */
    public List<PValue<?>> toList() {
        return new ArrayList<>(map.keySet());
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
    public PBool contains(PValue<?> item) {
        return new PBool(map.containsKey(item));
    }

    @Override
    public PMap clone() {
        return new PMap(map);
    }

    @Override
    protected String _asString() {
        StringBuilder sb = new StringBuilder();
        sb.append("{");
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
        sb.append("}");
        return sb.toString();
    }

    @Override
    public PMap getDefault() {
        return new PMap();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;

        if (!(obj instanceof PMap other)) return false;

        if (map.size() != other.map.size()) {
            return false;
        }

        for (PValue<?> key : map.keySet()) {
            if (!other.map.containsKey(key)) {
                return false;
            } else if (PValue.notEqual(other.map.get(key), this.map.get(key))) {
                return false;
            }
        }
        return true;
    }
}
