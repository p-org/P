package pexplicit.values;

import pexplicit.utils.exceptions.PExplicitRuntimeException;
import pexplicit.values.exceptions.InvalidIndexException;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

/**
 * Represents the PValue for P set
 */
public class PSet extends PCollection {
    private final List<PValue<?>> entries;
    private final Set<PValue<?>> unique_entries;

    /**
     * Constructor
     *
     * @param input_set the list of PValues to be added in this PSet.
     */
    public PSet(List<PValue<?>> input_set) {
        entries = new ArrayList<>();
        unique_entries = new HashSet<>();
        for (PValue<?> entry : input_set) {
            insertValue(PValue.clone(entry));
        }
    }

    /**
     * Copy constructor
     *
     * @param other value to copy from.
     */
    public PSet(PSet other) {
        entries = new ArrayList<>();
        unique_entries = new HashSet<>();
        for (PValue<?> entry : other.entries) {
            insertValue(PValue.clone(entry));
        }
    }

    /**
     * Get value at a given index.
     *
     * @param index index to get value at.
     * @return value at the index.
     * @throws InvalidIndexException
     */
    public PValue<?> getValue(int index) throws InvalidIndexException {
        if (index >= entries.size() || index < 0) throw new InvalidIndexException(index, this);
        return entries.get(index);
    }

    /**
     * Set value at a given index.
     *
     * @param index index to set value at.
     * @param val
     * @throws PExplicitRuntimeException
     */
    public void setValue(int index, PValue<?> val) throws PExplicitRuntimeException {
        throw new PExplicitRuntimeException("Set value of a set is not allowed!");
    }

    /**
     * Insert value to a PSet.
     *
     * @param val Value to insert at.
     */
    public void insertValue(PValue<?> val) {
        if (!this.contains(val)) {
            unique_entries.add(val);
            entries.add(val);
        }
    }

    /**
     * Get list of elements in the PSet.
     *
     * @return List of values
     */
    public List<PValue<?>> toList() {
        return entries;
    }

    @Override
    public PSet clone() {
        return new PSet(entries);
    }

    @Override
    public int hashCode() {
        return ComputeHash.getHashCode(unique_entries);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;

        if (!(obj instanceof PSet)) {
            return false;
        }

        return unique_entries.equals(((PSet) obj).unique_entries);
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append("(");
        String sep = "";
        for (PValue<?> item : entries) {
            sb.append(sep);
            sb.append(item);
            sep = ", ";
        }
        sb.append(")");
        return sb.toString();
    }

    @Override
    public int size() {
        return unique_entries.size();
    }

    @Override
    public boolean contains(PValue<?> item) {
        return unique_entries.contains(item);
    }
}
