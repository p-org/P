/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package mop;

import java.util.Set;
import java.util.HashSet;

public class PrtSet implements IValue<PrtSet> {
    private Set<IValue<?>> internalSet;

    public PrtSet() {
        internalSet = new HashSet<IValue<?>>();
    }

    private PrtSet(Set<IValue<?>> setValue) {
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

        if (!(obj instanceof PrtSet)) {
            return false;
        }

        PrtSet other = (PrtSet) obj;
        return internalSet.equals(other.internalSet);
    }

    @Override
    public PrtSet genericClone() {
        Set<IValue<?>> clonedSet = new HashSet<IValue<?>>();
        for (IValue<?> item : internalSet) {
            clonedSet.add(IValue.safeClone(item));
        }
        return new PrtSet(clonedSet);
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
}
