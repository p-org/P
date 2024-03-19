package pcover.values;

import lombok.Getter;

/**
 * Represents the PValue for P integer
 */
@Getter
public class PInt extends PValue<PInt> {
    private final int value;

    /**
     * Constructor
     *
     * @param val integer value to set to
     */
    public PInt(int val) {
        value = val;
    }

    /**
     * Constructor
     *
     * @param val object from where value to set to
     */
    public PInt(Object val) {
        if (val instanceof PInt)
            value = ((PInt) val).value;
        else
            value = (int) val;
    }

    /**
     * Copy constructor
     *
     * @param val value to copy from
     */
    public PInt(PInt val) {
        value = val.value;
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
