package pexplicit.values;

import pexplicit.values.exceptions.InvalidIndexException;

import java.util.ArrayList;
import java.util.Collection;
import java.util.List;

/**
 * Represents the PValue for P list/sequence
 */
public class PSeq<T extends PValue<T>> extends PCollection<T> {
    private final List<T> seq;

    /**
     * Constructor
     *
     * @param input_seq list of elements
     */
    public PSeq(List<T> input_seq) {
        seq = new ArrayList<>();
        for (T entry : input_seq) {
            seq.add(PValue.clone(entry));
        }
    }

    /**
     * Copy constructor
     *
     * @param other Value to copy from.
     */
    public PSeq(PSeq<T> other) {
        this(other.seq);
    }

    /**
     * Empty constructor
     */
    public PSeq() {
        this(new ArrayList<>());
    }

    /**
     * Get the value at a given index.
     *
     * @param index index to get the value at.
     * @return value at the index
     * @throws InvalidIndexException
     */
    public T get(PInt index) throws InvalidIndexException {
        if (index.getValue() >= seq.size() || index.getValue() < 0) throw new InvalidIndexException(index.getValue(), this);
        return seq.get(index.getValue());
    }

    /**
     * Set the value at a given index.
     *
     * @param index index to set the value at.
     * @param val   value to set to
     * @throws InvalidIndexException
     */
    public PSeq<T> set(PInt index, T val) throws InvalidIndexException {
        if (index.getValue() >= seq.size() || index.getValue() < 0) throw new InvalidIndexException(index.getValue(), this);
        List<T> newSeq = new ArrayList<>(seq);
        newSeq.set(index.getValue(), val);
        return new PSeq<>(newSeq);
    }

    /**
     * Insert a value at a given index.
     *
     * @param index index to insert the value at.
     * @param val   value to insert at the index.
     * @throws InvalidIndexException
     */
    public PSeq<T> add(PInt index, T val) throws InvalidIndexException {
        if (index.getValue() > seq.size() || index.getValue() < 0) throw new InvalidIndexException(index.getValue(), this);
        List<T> newSeq = new ArrayList<>(seq);
        newSeq.add(index.getValue(), val);
        return new PSeq<>(newSeq);
    }

    /**
     * Remove a value at a given index.
     *
     * @param index index to remove the value at.
     * @throws InvalidIndexException
     */
    public PSeq<T> removeAt(PInt index) throws InvalidIndexException {
        if (index.getValue() >= seq.size() || index.getValue() < 0) throw new InvalidIndexException(index.getValue(), this);
        List<T> newSeq = new ArrayList<>(seq);
        newSeq.remove(index.getValue());
        return new PSeq<>(newSeq);
    }

    /**
     * Convert the PSeq to a List of PValues.
     *
     * @return List of PValues corresponding to the PSeq.
     */
    public List<T> toList() {
        return seq;
    }

    @Override
    public PSeq<T> clone() {
        return new PSeq(seq);
    }

    @Override
    public int hashCode() {
        return ComputeHash.getHashCode((Collection<PValue<?>>) seq);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;

        if (!(obj instanceof PSeq)) {
            return false;
        }

        PSeq<T> other = (PSeq) obj;
        if (seq.size() != other.seq.size()) {
            return false;
        }

        for (int i = 0; i < seq.size(); i++) {
            if (!PValue.equals((PValue<?>) other.seq.get(i), (PValue<?>) this.seq.get(i))) {
                return false;
            }
        }
        return true;
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append("[");
        String sep = "";
        for (T item : seq) {
            sb.append(sep);
            sb.append(item);
            sep = ", ";
        }
        sb.append("]");
        return sb.toString();
    }

    @Override
    public PInt size() {
        return new PInt(seq.size());
    }

    @Override
    public PBool contains(T item) {
        return new PBool(seq.contains(item));
    }
}
