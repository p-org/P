package pex.values;

import pex.values.exceptions.InvalidIndexException;

import java.util.ArrayList;
import java.util.List;

/**
 * Represents the PValue for P list/sequence
 */
public class PSeq extends PValue<PSeq> implements PCollection {
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
        initialize();
    }

    /**
     * Copy constructor
     *
     * @param other Value to copy from.
     */
    public PSeq(PSeq other) {
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
    public PValue<?> get(PInt index) throws InvalidIndexException {
        if (index.getValue() >= seq.size() || index.getValue() < 0)
            throw new InvalidIndexException(index.getValue(), this);
        return seq.get(index.getValue());
    }

    /**
     * Set the value at a given index.
     *
     * @param index index to set the value at.
     * @param val   value to set to
     * @throws InvalidIndexException
     */
    public PSeq set(PInt index, PValue<?> val) throws InvalidIndexException {
        if (index.getValue() >= seq.size() || index.getValue() < 0)
            throw new InvalidIndexException(index.getValue(), this);
        List<PValue<?>> newSeq = new ArrayList<>(seq);
        newSeq.set(index.getValue(), val);
        return new PSeq(newSeq);
    }

    /**
     * Insert a value at a given index.
     *
     * @param index index to insert the value at.
     * @param val   value to insert at the index.
     * @throws InvalidIndexException
     */
    public PSeq add(PInt index, PValue<?> val) throws InvalidIndexException {
        if (index.getValue() > seq.size() || index.getValue() < 0)
            throw new InvalidIndexException(index.getValue(), this);
        List newSeq = new ArrayList<>(seq);
        newSeq.add(index.getValue(), val);
        return new PSeq(newSeq);
    }

    /**
     * Remove a value at a given index.
     *
     * @param index index to remove the value at.
     * @throws InvalidIndexException
     */
    public PSeq removeAt(PInt index) throws InvalidIndexException {
        if (index.getValue() >= seq.size() || index.getValue() < 0)
            throw new InvalidIndexException(index.getValue(), this);
        List newSeq = new ArrayList<>(seq);
        newSeq.remove(index.getValue());
        return new PSeq(newSeq);
    }

    /**
     * Convert the PSeq to a List of PValues.
     *
     * @return List of PValues corresponding to the PSeq.
     */
    public List<PValue<?>> toList() {
        return new ArrayList<>(seq);
    }

    @Override
    public PSeq clone() {
        return new PSeq(seq);
    }

    @Override
    protected String _asString() {
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
    public PSeq getDefault() {
        return new PSeq();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;

        if (!(obj instanceof PSeq other)) {
            return false;
        }

        if (seq.size() != other.seq.size()) {
            return false;
        }

        for (int i = 0; i < seq.size(); i++) {
            if (PValue.notEqual(other.seq.get(i), this.seq.get(i))) {
                return false;
            }
        }
        return true;
    }

    @Override
    public PInt size() {
        return new PInt(seq.size());
    }

    @Override
    public PBool contains(PValue<?> item) {
        return new PBool(seq.contains(item));
    }
}
