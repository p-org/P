/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */ 
package mop;

public class StringValue implements IValue<StringValue> {
    private String value;

    public StringValue(String value) {
        if (null == value) {
            throw new NullPointerException("Null value in the StringValue constructor.");
        }
        this.value = value;
    }

    public String get() {
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

        return this.value.equals(other.get());
    }

    @Override
    public StringValue genericClone() {
        return new StringValue(value);
    }

    @Override
    public String toString() {
        return value;
    }
}
