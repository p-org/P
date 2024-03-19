package pcover.values;

import lombok.Getter;

/**
 * Represents the PValue for P float
 */
@Getter
public class PFloat extends PValue<PFloat> {
    private final double value;

    /**
     * Constructor
     *
     * @param val Value to set to
     */
    public PFloat(double val) {
        value = val;
    }

    /**
     * Constructor
     *
     * @param val Value to set to
     */

    public PFloat(Object val) {
        if (val instanceof PFloat)
            value = ((PFloat) val).value;
        else
            value = (double) val;
    }

    /**
     * Copy constructor
     *
     * @param val Value to set to
     */
    public PFloat(PFloat val) {
        value = val.value;
    }

    @Override
    public PFloat clone() {
        return new PFloat(value);
    }

    @Override
    public int hashCode() {
        return Double.valueOf(value).hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof PFloat)) {
            return false;
        }
        return this.value == ((PFloat) obj).value;
    }

    @Override
    public String toString() {
        return Double.toString(value);
    }
}
