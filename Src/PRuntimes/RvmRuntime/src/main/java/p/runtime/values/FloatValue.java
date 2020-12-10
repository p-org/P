/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package p.runtime.values;

public class FloatValue implements IValue<FloatValue> {
    private double value;

    public FloatValue(double value) {
        this.value = value;
    }

    public double getValue() {
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
        return this.value == other.getValue();
    }

    @Override
    public FloatValue genericClone() {
        return new FloatValue(value);
    }

    @Override
    public String toString() {
        return Double.toString(value);
    }

    // Constructor and setter only used for JSON deserialization
    public FloatValue() {}

    public void setValue(double value) {
        this.value = value;
    }

}
