/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package mop;

import java.util.Map;
import java.util.HashMap;

public class PrtMap implements IValue<PrtMap> {
    private Map<IValue<?>, IValue<?>> internalMap;

    public PrtMap() {
        internalMap = new HashMap<IValue<?>, IValue<?>>();
    }

    private PrtMap(Map<IValue<?>, IValue<?>> map) {
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

    public PrtSeq cloneKeys() {
        return new PrtSeq(internalMap.keySet());
    }

    public PrtSeq cloneValues() {
        return new PrtSeq(internalMap.values());
    }

    @Override
    public int hashCode() {
        return internalMap.hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;

        if (!(obj instanceof PrtMap)) {
            return false;
        }

        PrtMap other = (PrtMap) obj;
        return internalMap.equals(other.internalMap);
    }

    @Override
    public PrtMap genericClone() {
        Map<IValue<?>, IValue<?>> clonedMap = new HashMap<IValue<?>, IValue<?>>();
        for (Map.Entry<IValue<?>, IValue<?>> entry : internalMap.entrySet()) {
            clonedMap.put(IValue.safeClone(entry.getKey()), IValue.safeClone(entry.getValue()));
        }
        return new PrtMap(clonedMap);
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
}
