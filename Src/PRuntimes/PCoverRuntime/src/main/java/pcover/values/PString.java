package pcover.values;

import lombok.Getter;

/**
 * Represents the PValue for P string
 */
@Getter
public class PString extends PValue<PString> {
    private final String value;

    /**
     * Constructor
     *
     * @param val String value to set to.
     */
    public PString(String val) {
        value = val;
    }

    /**
     * Constructor
     *
     * @param val PString value to construct from.
     */
    public PString(PString val) {
        value = val.value;
    }

    /**
     * Constructor
     *
     * @param val Object value to construct from.
     */
    public PString(Object val) {
        if (val instanceof PString) value = ((PString) val).value;
        else value = (String) val;
    }

    @Override
    public PString clone() {
        return new PString(value);
    }

    @Override
    public int hashCode() {
        return value.hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof PString)) {
            return false;
        }
        return this.value.equals(((PString) obj).value);
    }

    @Override
    public String toString() {
        return value;
    }
}
