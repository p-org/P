/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */
package p.runtime.values;

public class EnumValue<T extends java.lang.Enum> implements IValue<EnumValue<T>> {
    private T value;

    public EnumValue(T value) {
        this.value = value;
    }

    public T getValue() {
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

        if (!(obj instanceof EnumValue)) {
            return false;
        }

        EnumValue<?> other = (EnumValue) obj;
        if (!value.getClass().equals(other.value.getClass())){
            if (!value.getClass().getSimpleName().equals(other.value.getClass().getSimpleName())) {
                return false;
            }
            return this.value.ordinal() == other.value.ordinal();
        }
        return this.value.equals(other.value);
    }

    @Override
    public EnumValue<T> genericClone() {
        return new EnumValue<T>(value);
    }

    @Override
    public String toString() {
        return value.toString();
    }

    public EnumValue() {}

    public void setValue(T value) {
        this.value = value;
    }
}
