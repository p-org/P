/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package p.runtime.values;

import java.util.Set;
import java.util.HashSet;

public class PSet implements IValue<PSet> {
    private Set<IValue<?>> internalSet;

    public PSet() {
        internalSet = new HashSet<IValue<?>>();
    }

    private PSet(Set<IValue<?>> setValue) {
        this.internalSet = setValue;
    }

    public int size() {
        return internalSet.size();
    }

    public void insert(IValue<?> item) {
        internalSet.add(IValue.safeClone(item));
    }

    public void remove(IValue<?> item) {
        internalSet.remove(item);
    }

    public boolean contains(IValue<?> item) {
        return internalSet.contains(item);
    }

    @Override
    public int hashCode() {
        return internalSet.hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;

        if (!(obj instanceof PSet)) {
            return false;
        }

        PSet other = (PSet) obj;
        return internalSet.equals(other.internalSet);
    }

    @Override
    public PSet genericClone() {
        Set<IValue<?>> clonedSet = new HashSet<IValue<?>>();
        for (IValue<?> item : internalSet) {
            clonedSet.add(IValue.safeClone(item));
        }
        return new PSet(clonedSet);
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append("(");
	    String sep = "";
        for (IValue<?> item : internalSet) {
            sb.append(sep);
            sb.append("<");
            sb.append(item);
            sb.append(">");
            sep = ", ";
        }
        sb.append(")");
        return sb.toString();
    }

    // getter and setter only used for JSON deserialization
    public Set<IValue<?>> getInternalSet() {
        return this.internalSet;
    }

    public void setInternalSet(Set<IValue<?>> set) {
        this.internalSet = set;
    }

}
