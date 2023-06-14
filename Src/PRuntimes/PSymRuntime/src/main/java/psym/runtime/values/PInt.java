package psym.runtime.values;

public class PInt extends PValue<PInt> {
    // stores the int value
    private final int value;

    public PInt(int val) {
        value = val;
    }

    public PInt(Object val) {
        if (val instanceof PInt)
            value = ((PInt) val).value;
        else
            value = (int) val;
    }

    public PInt(PInt val) {
        value = val.value;
    }

    public int getValue() {
        return value;
    }

    @Override
    public PInt clone() {
        return new PInt(value);
    }

    @Override
    public int hashCode() {
        return ((Integer) value).hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof PInt)) {
            return false;
        }
        return this.value == ((PInt) obj).value;
    }

    @Override
    public String toString() {
        return Long.toString(value);
    }
}
