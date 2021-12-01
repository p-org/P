package p.runtime.values;

import lombok.Getter;
import lombok.NonNull;

public class PEnum extends PValue<PEnum> {
    // stores the int value
    @Getter
    private final String name;
    @Getter
    private final int value;

    public PEnum(String name, int val)
    {
        this.name = name; value = val;
    }

    public PEnum(@NonNull PEnum val)
    {
        name = val.name; value = val.value;
    }

    @Override
    public PEnum clone() {
        return new PEnum(this);
    }

    @Override
    public int hashCode() {
        return name.hashCode() ^ Long.hashCode(value);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof PEnum)) {
            return false;
        }
        return this.value == ((PEnum)obj).value && this.name.equals(((PEnum)obj).name);
    }

    @Override
    public String toString() {
        return name;
    }
}
