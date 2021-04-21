/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package p.runtime.values;

import java.util.Map;
import java.util.HashMap;

import p.runtime.exceptions.MapInsertError;

public class PMap implements IValue<PMap> {
    private Map<IValue<?>, IValue<?>> internalMap;

    public PMap() {
        internalMap = new HashMap<IValue<?>, IValue<?>>();
    }

    private PMap(Map<IValue<?>, IValue<?>> map) {
        this.internalMap = map;
    }

    public int size() {
        return internalMap.size();
    }

    public void insert(IValue<?> key, IValue<?> value) {
        if (internalMap.containsKey(key)) {
            throw new MapInsertError("key '" + key.toString() + "' already exists.");
        }
        put(key, value);
    }

    public void put(IValue<?> key, IValue<?> value) {
        internalMap.put(IValue.safeClone(key), IValue.safeClone(value));
    }

    public IValue<?> get(IValue<?> key) {
        return internalMap.get(key);
    }

    public void remove(IValue<?> key) {
        internalMap.remove(key);
    }

    public boolean containsKey(IValue<?> key) {
        return internalMap.containsKey(key);
    }

    public PSeq cloneKeys() {
        return new PSeq(internalMap.keySet());
    }

    public PSeq cloneValues() {
        return new PSeq(internalMap.values());
    }

    @Override
    public int hashCode() {
        return internalMap.hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;

        if (!(obj instanceof PMap)) {
            return false;
        }

        PMap other = (PMap) obj;
        return internalMap.equals(other.internalMap);
    }

    @Override
    public PMap genericClone() {
        Map<IValue<?>, IValue<?>> clonedMap = new HashMap<IValue<?>, IValue<?>>();
        for (Map.Entry<IValue<?>, IValue<?>> entry : internalMap.entrySet()) {
            clonedMap.put(IValue.safeClone(entry.getKey()), IValue.safeClone(entry.getValue()));
        }
        return new PMap(clonedMap);
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append("(");
        String sep = "";
        for (Map.Entry<IValue<?>, IValue<?>> entry : internalMap.entrySet()) {
            sb.append(sep);
            sb.append("<");
            sb.append(entry.getKey());
            sb.append(" |-> ");
            sb.append(entry.getValue());
            sb.append(">");
            sep = ", ";
        }
        sb.append(")");
        return sb.toString();
    }

    // getter and setter only used for JSON deserialization
    public Map<IValue<?>, IValue<?>> getInternalMap() {
        return this.internalMap;
    }

    public void setInternalMap(Map<IValue<?>, IValue<?>> m) {
        this.internalMap = m;
    }

}
