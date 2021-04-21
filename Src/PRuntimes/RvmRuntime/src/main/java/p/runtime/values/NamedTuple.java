/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package p.runtime.values;

import java.util.Arrays;

public class NamedTuple implements IValue<NamedTuple> {
    private String[] fieldNames;
    private IValue<?>[] fieldValues;

    public NamedTuple(String[] fieldNames, IValue<?>[] fieldValues)
    {
        assert fieldNames.length == fieldValues.length;
        this.fieldNames = fieldNames;
        this.fieldValues = new IValue<?>[fieldValues.length];
        for (int i = 0; i < fieldValues.length; i++) {
            this.fieldValues[i] = IValue.safeClone(fieldValues[i]);
        }
    }

    public IValue<?> getField(String name) {
        for (int i = 0; i < fieldNames.length; i++) {
            if (name.equals(fieldNames[i])) {
                return fieldValues[i];
            }
        }
        assert false;
        return null;
    }

    public void setField(String name, IValue<?> value) {
        for (int i = 0; i < fieldNames.length; i++) {
            if (name.equals(fieldNames[i])) {
                fieldValues[i] = value;
                return;
            }
        }
        assert false;
    }

    public String[] getFieldNames() {
        return fieldNames;
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

        if (!(obj instanceof NamedTuple)) {
            return false;
        }

        NamedTuple other = (NamedTuple) obj;

        if (fieldValues.length != other.getFieldValues().length) {
             return false;
        }

        String[] otherFieldNames = other.getFieldNames();
        IValue<?>[] otherFieldValues = other.getFieldValues();
        for (int i = 0 ; i < fieldValues.length; i++) {
            if (!fieldNames[i].equals(otherFieldNames[i])) {
                return false;
            }
            if (!IValue.safeEquals(fieldValues[i], otherFieldValues[i])) {
                return false;
            }
        }
        return true;
    }

    @Override
    public NamedTuple genericClone() {
        return new NamedTuple(fieldNames, this.fieldValues);
    }

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append("(");
        boolean hadElements = false;
        for (int i = 0; i < fieldNames.length; i++) {
            if (hadElements) {
                sb.append(", ");
            }
            sb.append(fieldNames[i]);
            sb.append(": ");
            sb.append(fieldValues[i]);
            hadElements = true;
        }
        sb.append(")");
        return sb.toString();
    }

    // Constructor and setter only used for JSON deserialization
    public NamedTuple() {}

    public void setFieldNames(String[] fieldNames) {
        this.fieldNames = fieldNames;
    }

    public void setFieldValues(IValue<?>[] fieldValues) {
        this.fieldValues = fieldValues;
    }

}
