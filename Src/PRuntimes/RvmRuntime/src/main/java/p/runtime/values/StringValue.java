/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package p.runtime.values;

public class StringValue implements IValue<StringValue> {
    private String value;

    public StringValue(String value) {
        if (null == value) {
            throw new NullPointerException("Null value in the StringValue constructor.");
        }
        this.value = value;
    }

    public String getValue() {
        return value;
    }

    @Override
    public int hashCode() {
        return value.hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;

        if (!(obj instanceof StringValue)) {
            return false;
        }

        StringValue other = (StringValue) obj;

        return this.value.equals(other.getValue());
    }

    @Override
    public StringValue genericClone() {
        return new StringValue(value);
    }

    @Override
    public String toString() {
        return value;
    }

    // Constructor and setter only used for JSON deserialization
    public StringValue() {}

    public void setValue(String value) {
        this.value = value;
    }

}
