package p.runtime.values;

import lombok.Getter;
import lombok.NonNull;

public class PFloat extends PValue<PFloat> {
    // stores the int value
    @Getter
    private final double value;

    public PFloat(double val)
    {
        value = val;
    }

    public PFloat(@NonNull PFloat val)
    {
        value = val.value;
    }

    @Override
    public PFloat clone() {
        return new PFloat(value);
    }

    @Override
    public int hashCode() {
        return new Double(value).hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof PFloat)) {
            return false;
        }
        return this.value == ((PFloat)obj).value;
    }

    @Override
    public String toString() {
        return Double.toString(value);
    }
}
