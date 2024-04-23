package pexplicit.values;

import pexplicit.values.exceptions.TupleInvalidIndexException;

import java.util.Arrays;

/**
 * Represents the PValue for P unnamed tuple
 */
public class PTuple extends PValue<PTuple> {
    private final PValue<?>[] fields;

    /**
     * Creates a new PTuple with the given fields
     */
    public PTuple(PValue<?>... input_fields) {
        this.fields = new PValue<?>[input_fields.length];
        for (int i = 0; i < input_fields.length; i++) {
            this.fields[i] = PValue.clone(input_fields[i]);
        }
        setRep();
    }

    /**
     * Creates a new PTuple with the same fields as the other PTuple.
     */
    public PTuple(PTuple other) {
        this.fields = new PValue<?>[other.fields.length];
        for (int i = 0; i < other.fields.length; i++) {
            this.fields[i] = PValue.clone(other.fields[i]);
        }
        setRep();
    }

    /**
     * Returns the number of fields in the PTuple.
     */
    public int getArity() {
        return fields.length;
    }

    /**
     * Returns the field at the given index. Throws an exception if the index is invalid.
     */
    public PValue<?> getField(int index) throws TupleInvalidIndexException {
        if (index >= fields.length) throw new TupleInvalidIndexException(this, index);
        return fields[index];
    }

    /**
     * Sets the field at the given index. Throws an exception if the index is invalid.
     */
    public PTuple setField(int index, PValue<?> val) throws TupleInvalidIndexException {
        if (index >= fields.length) throw new TupleInvalidIndexException(this, index);
        PValue<?>[] newFields = fields.clone();
        newFields[index] = val;
        return new PTuple(newFields);
    }

    @Override
    public PTuple clone() {
        return new PTuple(fields);
    }

    @Override
    protected void setHashCode() {
        hashCode = Arrays.hashCode(fields);
    }

    @Override
    protected void setStringRep() {
        StringBuilder sb = new StringBuilder();
        sb.append("(");
        String sep = "";
        for (PValue<?> field : fields) {
            sb.append(sep);
            sb.append(field);
            sep = ", ";
        }
        sb.append(")");
        stringRep = sb.toString();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;

        if (!(obj instanceof PTuple other)) {
            return false;
        }

        if (fields.length != other.fields.length) {
            return false;
        }

        for (int i = 0; i < fields.length; i++) {
            if (PValue.notEqual(fields[i], other.fields[i])) {
                return false;
            }
        }
        return true;
    }
}
