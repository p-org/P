/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package mop;

public class IntValue implements IValue<IntValue> {
    private long value;

    public IntValue(long value) {
        this.value = value;
    }

    public long get() {
        return value;
    }

    @Override
    public int hashCode() {
        return (new Long(value)).hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;

        if (!(obj instanceof IntValue)) {
            return false;
        }

        IntValue other = (IntValue) obj;
        return this.value == other.get();
    }

    @Override
    public IntValue genericClone() {
        return new IntValue(value);
    }

    @Override
    public String toString() {
        return Long.toString(value);
    }
}
