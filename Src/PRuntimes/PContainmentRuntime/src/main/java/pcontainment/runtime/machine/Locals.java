package pcontainment.runtime.machine;

import com.microsoft.z3.Expr;
import com.microsoft.z3.SeqExpr;
import org.jetbrains.annotations.NotNull;
import org.jetbrains.annotations.Nullable;

import java.util.Collection;
import java.util.HashMap;
import java.util.Map;
import java.util.Set;

public class Locals {

    private final Map<String, Expr<?>> primitive_implementation = new HashMap<>();
    private final Map<String, DeterministicSeq<Expr<?>>> det_seqs = new HashMap<>();
    private final Map<String, DeterministicMap<Object, Expr<?>>> det_maps = new HashMap<>();

    public Locals() {}

    public Locals(Locals other) {
        primitive_implementation.putAll(other.primitive_implementation);
    }

    public Locals immutablePut(String key, Expr<?> value) {
        Locals ret = new Locals(this);
        ret.put(key, value);
        return ret;
    }

    public Locals immutableAdd(String key, Expr<?> value) {
        Locals ret = new Locals(this);
        ret.add(key, value);
        return ret;
    }

    public Locals immutableAdd(String key, int i, Expr<?> value) {
        Locals ret = new Locals(this);
        ret.add(key, i, value);
        return ret;
    }

    public Locals immutableSubseq(String key, int i) {
        Locals ret = new Locals(this);
        ret.subseq(key, i);
        return ret;
    }

    public Expr<?> get(String seq, int idx) {
        return det_seqs.get(seq).get(idx);
    }

    private void add(String seq, Expr<?> value) {
        det_seqs.put(seq, det_seqs.get(seq).immutableAdd(value));
    }

    private void add(String seq, int idx, Expr<?> value) {
        det_seqs.put(seq, det_seqs.get(seq).immutableAdd(idx, value));
    }

    private void subseq(String seq, int idx) {
        det_seqs.get(seq).immutableSubseq(idx);
    }

    public int size() {
        return primitive_implementation.size();
    }

    public boolean isEmpty() {
        return primitive_implementation.isEmpty();
    }

    public boolean containsKey(Object key) {
        return primitive_implementation.containsKey(key);
    }

    public boolean containsValue(Object value) {
        return primitive_implementation.containsValue(value);
    }

    public Expr<?> get(Object key) {
        return primitive_implementation.get(key);
    }

    public Expr<?> put(String key, Expr<?> value) {
        return primitive_implementation.put(key, value);
    }

    public Expr<?> remove(Object key) {
        return primitive_implementation.remove(key);
    }

    public void putAll(@NotNull Map<? extends String, ? extends Expr<?>> m) {
        primitive_implementation.putAll(m);
    }

    public void clear() {
        primitive_implementation.clear();
    }

    @NotNull
    public Set<String> keySet() { return primitive_implementation.keySet(); }

    @NotNull
    public Collection<Expr<?>> values() {
        return primitive_implementation.values();
    }

    @NotNull
    public Set<Map.Entry<String, Expr<?>>> entrySet() {
        return primitive_implementation.entrySet();
    }
}
