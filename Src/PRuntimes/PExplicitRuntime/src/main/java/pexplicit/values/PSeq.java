package pexplicit.values;

import pexplicit.values.exceptions.InvalidIndexException;

import java.util.ArrayList;
import java.util.List;

/**
 * Represents the PValue for P list/sequence
 */
public class PSeq extends PCollection {
    private final List<PValue<?>> seq;

    /**
     * Constructor
     *
     * @param input_seq list of elements
     */
    public PSeq(List<PValue<?>> input_seq) {
        seq = new ArrayList<>();
        for (PValue<?> entry : input_seq) {
            seq.add(PValue.clone(entry));
        }
    }

    /**
     * COpy constructor
     *
     * @param other Value to copy from.
     */
    public PSeq(PSeq other) {
        seq = new ArrayList<>();
        for (PValue<?> entry : other.seq) {
            seq.add(PValue.clone(entry));
        }
    }

    /**
     * Get the value at a given index.
     *
     * @param index index to get the value at.
     * @return value at the index
     * @throws InvalidIndexException
     */
    public PValue<?> getValue(int index) throws InvalidIndexException {
        if (index >= seq.size() || index < 0) throw new InvalidIndexException(index, this);
        return seq.get(index);
    }

    /**
     * Set the value at a given index.
     *
     * @param index index to set the value at.
     * @param val   value to set to
     * @throws InvalidIndexException
     */
    public void setValue(int index, PValue<?> val) throws InvalidIndexException {
        if (index >= seq.size() || index < 0) throw new InvalidIndexException(index, this);
        seq.set(index, val);
    }

    /**
     * Insert a value at a given index.
     *
     * @param index index to insert the value at.
     * @param val   value to insert at the index.
     * @throws InvalidIndexException
     */
    public void insertValue(int index, PValue<?> val) throws InvalidIndexException {
        if (index > seq.size() || index < 0) throw new InvalidIndexException(index, this);
        seq.add(index, val);
    }

    /**
     * Convert the PSeq to a List of PValues.
     *
     * @return List of PValues corresponding to the PSeq.
     */
    public List<PValue<?>> toList() {
        return seq;
    }

    @Override
    public PSeq clone() {
        return new PSeq(seq);
    }

    @Override
    public int hashCode() {
        return ComputeHash.getHashCode(seq);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;

        if (!(obj instanceof PSeq)) {
            return false;
        }

        PSeq other = (PSeq) obj;
        if (seq.size() != other.seq.size()) {
            return false;
        }

        for (int i = 0; i < seq.size(); i++) {
            if (PValue.equals(other.seq.get(i), this.seq.get(i))) {
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
        for (PValue<?> item : seq) {
            sb.append(sep);
            sb.append(item);
            sep = ", ";
        }
        sb.append("]");
        return sb.toString();
    }

    @Override
    public int size() {
        return seq.size();
    }

    @Override
    public boolean contains(PValue<?> item) {
        return seq.contains(item);
    }
}
