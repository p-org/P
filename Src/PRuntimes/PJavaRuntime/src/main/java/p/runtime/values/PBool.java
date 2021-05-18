package p.runtime.values;

import lombok.NonNull;

public class PBool extends PValue<PBool>{
    // stores the int value
    private final boolean value;

    public boolean getValue() {
        return value;
    }

    public PBool(boolean val)
    {
        value = val;
    }

    public PBool(@NonNull PBool val)
    {
        value = val.value;
    }

    @Override
    public PBool clone() {
        return new PBool(value);
    }

    @Override
    public int hashCode() {
        return new Boolean(value).hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof PBool)) {
            return false;
        }
        return this.value == ((PBool)obj).value;
    }

    @Override
    public String toString() {
        return Boolean.toString(value);
    }
}
