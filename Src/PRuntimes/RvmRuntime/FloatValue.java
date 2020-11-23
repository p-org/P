/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package mop;

public class FloatValue implements IValue<FloatValue> {
    private double value;

    public FloatValue(double value) {
        this.value = value;
    }

    public double get() {
        return value;
    }

    @Override
    public int hashCode() {
        return (new Double(value)).hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;

        if (!(obj instanceof FloatValue)) {
            return false;
        }

        FloatValue other = (FloatValue) obj;
        return this.value == other.get();
    }

    @Override
    public FloatValue genericClone() {
        return new FloatValue(value);
    }

    @Override
    public String toString() {
        return Double.toString(value);
    }
}
