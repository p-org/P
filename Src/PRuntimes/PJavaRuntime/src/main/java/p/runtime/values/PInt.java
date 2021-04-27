package p.runtime.values;
import lombok.NonNull;

public class PInt extends PValue<PInt> {
    // stores the int value
    private final long value;

    public PInt(long val)
    {
        value = val;
    }

    public PInt(@NonNull PInt val)
    {
        value = val.value;
    }

    /**
     * Get the int value in PInt
     * @return int value
     */
    public long intValue()
    {
        return value;
    }

    @Override
    public PInt clone() {
        return new PInt(value);
    }

    @Override
    public int hashCode() {
        return (new Long(value)).hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof PInt)) {
            return false;
        }
        return this.value == ((PInt)obj).value;
    }



    @Override
    public String toString() {
        return Long.toString(value);
    }
}
