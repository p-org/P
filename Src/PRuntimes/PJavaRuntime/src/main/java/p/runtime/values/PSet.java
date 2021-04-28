package p.runtime.values;

import lombok.NonNull;
import lombok.SneakyThrows;
import p.runtime.values.exceptions.ComparingPValuesException;
import p.runtime.values.exceptions.InvalidIndexException;
import p.runtime.values.exceptions.KeyNotFoundException;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

public class PSet extends PCollection {
    // stores the map
    private final List<PValue<?>> set;

    public PSet(Set<PValue<?>> input_set)
    {
        set = new ArrayList<>();
        for (var entry : input_set) {
            set.add(PValue.clone(entry));
        }
    }

    public PSet(@NonNull PSet other)
    {
        set = new ArrayList<>();
        for (var entry : other.set) {
            set.add(PValue.clone(entry));
        }
    }

    public PValue<?> getValue(int index) throws InvalidIndexException {
        if(index >= set.size() || index < 0)
            throw new InvalidIndexException(index, this);
        return set.get(index);
    }

    public void setValue(int index, PValue<?> val) throws InvalidIndexException {
        if(index >= set.size() || index < 0)
            throw new InvalidIndexException(index, this);
        set.set(index, val);
    }

    public void insertValue(int index, PValue<?> val) throws InvalidIndexException {
        if(index > set.size() || index < 0)
            throw new InvalidIndexException(index, this);
        set.add(index, val);
    }

    @Override
    public PSet clone() {
        return new PSet(seq);
    }

    @Override
    public int hashCode() {
        return ComputeHash.getHashCode(seq);
    }

    @SneakyThrows
    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;

        if (!(obj instanceof PSet other)) {
            return false;
        }

        if (set.size() != other.set.size()) {
            return false;
        }
        for (int i = 0; i<set.size(); i++) {
            if (!PValue.equals(other.set.get(i), this.set.get(i))) {
                return false;
            }
        }
        return true;
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append("(");
        String sep = "";
        for (PValue<?> item : seq) {
            sb.append(sep);
            sb.append(item);
            sep = ", ";
        }
        sb.append(")");
        return sb.toString();
    }

    @Override
    public int size() {
        return set.size();
    }

    @Override
    public boolean contains(PValue<?> item) {
        return set.contains(item);
    }
}
