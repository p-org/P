/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package p.runtime.values;

import java.util.Arrays;

public class Tuple implements IValue<Tuple> {
    private IValue<?>[] fieldValues;

    public Tuple(IValue<?>[] fieldValues)
    {
        this.fieldValues = new IValue<?>[fieldValues.length];
        for (int i = 0; i < fieldValues.length; i++) {
            IValue<?> value = fieldValues[i];
            if (value != null) {
                value = value.genericClone();
            }
            this.fieldValues[i] = value;
        }
    }

    public IValue<?> getField(int index) {
        return fieldValues[index];
    }

    public void setField(int index, IValue<?> value) {
        fieldValues[index] = value;
    }

    public IValue<?>[] getFieldValues() {
        return fieldValues;
    }

    @Override
    public int hashCode() {
        return Arrays.hashCode(fieldValues);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;

        if (!(obj instanceof Tuple)) {
            return false;
        }

        Tuple other = (Tuple) obj;

        if (fieldValues.length != other.fieldValues.length) {
             return false;
        }

        IValue<?>[] otherFieldValues = other.fieldValues;
        for (int i = 0 ; i < fieldValues.length; i++) {
            if (!IValue.safeEquals(fieldValues[i], otherFieldValues[i])) {
                return false;
            }
        }
        return true;
    }

    @Override
    public Tuple genericClone() {
        return new Tuple(this.fieldValues);
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append("(");
        String sep = "";
        for (int i = 0; i < fieldValues.length; i++) {
            sb.append(sep);
            sb.append(fieldValues[i]);
            sep = ", ";
        }
        sb.append(")");
        return sb.toString();
    }

    // Constructor and setter only used for JSON deserialization
    public Tuple() {}

    public void setFieldValues(IValue<?>[] fieldValues) {
        this.fieldValues = fieldValues;
    }

}
