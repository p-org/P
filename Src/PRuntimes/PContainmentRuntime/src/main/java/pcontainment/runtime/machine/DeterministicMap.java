package pcontainment.runtime.machine;

import com.microsoft.z3.Expr;
import lombok.Getter;
import org.jetbrains.annotations.NotNull;

import java.util.*;

public class DeterministicMap<K, V extends Expr<?>> {
    @Getter
    private final String name;
    private Map<K, V> map = new HashMap<>();

    public DeterministicMap(String name) {
        this.name = name;
    }

    public DeterministicMap(DeterministicMap<K, V> other) {
        this.name = other.name;
        map = new HashMap<>(other.map);
    }

    public DeterministicMap<K, V> immutablePut(K key, V value) {
        DeterministicMap<K, V> ret = new DeterministicMap<>(this);
        ret.put(key, value);
        return ret;
    }

    public DeterministicMap<K, V> immutablePutAll(@NotNull Map<? extends K, ? extends V> m) {
        DeterministicMap<K, V> ret = new DeterministicMap<>(this);
        ret.putAll(m);
        return ret;
    }

    public DeterministicMap<K, V> immutableRemove(K key, V value) {
        DeterministicMap<K, V> ret = new DeterministicMap<>(this);
        ret.remove(key);
        return ret;
    }

    public DeterministicMap<K, V> immutableClear() {
        return new DeterministicMap<>(this.name);
    }

    public int size() {
        return map.size();
    }

    public boolean isEmpty() {
        return map.isEmpty();
    }

    public boolean containsKey(Object key) {
        return map.containsKey(key);
    }

    public boolean containsValue(Object value) {
        return map.containsValue(value);
    }

    public Expr<?> get(Object key) {
        return map.get(key);
    }

    private Expr<?> put(K key, V value) {
        return map.put(key, value);
    }

    private Expr<?> remove(Object key) {
        return map.remove(key);
    }

    private void putAll(@NotNull Map<? extends K, ? extends V> m) {
        map.putAll(m);
    }

    @NotNull
    public Set<K> keySet() { return map.keySet(); }

    @NotNull
    public Collection<V> values() {
        return map.values();
    }

    @NotNull
    public Set<Map.Entry<K, V>> entrySet() {
        return map.entrySet();
    }
}
