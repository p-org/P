package pexplicit.values;

import pexplicit.utils.exceptions.PExplicitRuntimeException;
import pexplicit.values.exceptions.InvalidIndexException;

import java.util.*;

/**
 * Represents the PValue for P set
 */
public class PSet extends PValue<PSet> implements PCollection {
    private final List<PValue<?>> entries;
    private final Set<PValue<?>> unique_entries;

    /**
     * Constructor
     *
     * @param input_set the list of PValues to be added in this PSet.
     */
    public PSet(List<PValue<?>> input_set) {
        entries = new ArrayList<>(input_set);
        unique_entries = new HashSet<>(input_set);
    }

    /**
     * Copy constructor
     *
     * @param other value to copy from.
     */
    public PSet(PSet other) {
        this(other.entries);
    }

    /**
     * Empty constructor
     */
    public PSet() {
        this(new ArrayList<>());
    }

    /**
     * Get value at a given index.
     *
     * @param index index to get value at.
     * @return value at the index.
     * @throws InvalidIndexException
     */
    public PValue<?> get(PInt index) throws InvalidIndexException {
        if (index.getValue() >= entries.size() || index.getValue() < 0) throw new InvalidIndexException(index.getValue(), this);
        return entries.get(index.getValue());
    }

    /**
     * Set value at a given index.
     *
     * @param index index to set value at.
     * @param val
     * @throws PExplicitRuntimeException
     */
    public PSet set(PInt index, PValue<?> val) throws PExplicitRuntimeException {
        throw new PExplicitRuntimeException("Set value of a set is not allowed!");
    }

    /**
     * Add value to a PSet.
     *
     * @param val Value to insert at.
     */
    public PSet add(PValue<?> val) {
        if (unique_entries.contains(val)) {
            return this;
        }
        List<PValue<?>> newEntries = new ArrayList<>(entries);
        newEntries.add(val);
        return new PSet(newEntries);
    }

    /**
     * Remove value from a PSet.
     *
     * @param val Value to remove.
     */
    public PSet remove(PValue<?> val) {
        if (!unique_entries.contains(val)) {
            return this;
        }
        List<PValue<?>> newEntries = new ArrayList<>(entries);
        newEntries.remove(val);
        return new PSet(newEntries);
    }

    /**
     * Get list of elements in the PSet.
     *
     * @return List of values
     */
    public List<PValue<?>> toList() {
        return new ArrayList<>(entries);
    }

    @Override
    public PSet clone() {
        return new PSet(entries);
    }

    @Override
    public int hashCode() {
        return ComputeHash.getHashCode((Collection<PValue<?>>) unique_entries);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;

        if (!(obj instanceof PSet)) {
            return false;
        }

        PSet other = (PSet) obj;
        if (unique_entries.size() != other.unique_entries.size()) {
            return false;
        }

        for (PValue<?> entry : unique_entries) {
            if (!other.unique_entries.contains(entry)) {
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
        for (PValue<?> item : entries) {
            sb.append(sep);
            sb.append(item);
            sep = ", ";
        }
        sb.append(")");
        return sb.toString();
    }

    @Override
    public PInt size() {
        return new PInt(unique_entries.size());
    }

    @Override
    public PBool contains(PValue<?> item) {
        return new PBool(unique_entries.contains(item));
    }
}
