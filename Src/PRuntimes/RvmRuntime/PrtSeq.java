/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package mop;

import java.lang.Iterable;
import java.util.List;
import java.util.ArrayList;

public class PrtSeq implements IValue<PrtSeq> {
    private List<IValue<?>> internalList;

    public PrtSeq() {
        internalList = new ArrayList<IValue<?>>();
    }

    public PrtSeq(Iterable<IValue<?>> seq) {
        internalList = new ArrayList<IValue<?>>();
        for (IValue<?> item : seq) {
            internalList.add(IValue.safeClone(item));
        }
    }

    public int size() {
        return internalList.size();
    }

    public void insert(int index, IValue<?> item) {
        internalList.add(index, IValue.safeClone(item));
    }

    public void setIndex(int index, IValue<?> item) {
        internalList.set(index, IValue.safeClone(item));
    }

    public IValue<?> get(int index) {
        return internalList.get(index);
    }

    public void remove(int index) {
        internalList.remove(index);
    }

    public boolean contains(IValue<?> item) {
        return internalList.contains(item);
    }

    @Override
    public int hashCode() {
        return internalList.hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;

        if (!(obj instanceof PrtSeq)) {
            return false;
        }

        PrtSeq other = (PrtSeq) obj;
        return internalList.equals(other.internalList);
    }

    @Override
    public PrtSeq genericClone() {
        return new PrtSeq(this.internalList);
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append("(");
	    String sep = "";
        for (IValue<?> item : internalList) {
            sb.append(sep);
            sb.append(item);
            sep = ", ";
        }
        sb.append(")");
        return sb.toString();
    }
}
