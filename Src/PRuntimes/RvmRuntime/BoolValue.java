/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package mop;

public class BoolValue implements IValue<BoolValue> {
    private boolean value;

    public BoolValue(boolean value) {
        this.value = value;
    }

    public boolean get() {
        return value;
    }

    @Override
    public int hashCode() {
        return (new Boolean(value)).hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;

        if (!(obj instanceof BoolValue)) {
            return false;
        }

        BoolValue other = (BoolValue) obj;
        return this.value == other.get();
    }

    @Override
    public BoolValue genericClone() {
        return new BoolValue(value);
    }

    @Override
    public String toString() {
        return Boolean.toString(value);
    }
}
