package pexplicit.values;

/**
 * Represents the PValue for P boolean
 */
public class PBool extends PValue<PBool> {
    public static final PBool PTRUE = new PBool(true);
    public static final PBool PFALSE = new PBool(false);

    private final boolean value;

    /**
     * Constructor
     *
     * @param val boolean value to set to
     */
    public PBool(boolean val) {
        value = val;
    }

    /**
     * Constructor
     *
     * @param val Object to set to
     */
    public PBool(Object val) {
        if (val instanceof PBool) value = ((PBool) val).value;
        else value = (boolean) val;
    }

    /**
     * Copy constructor.
     *
     * @param val value to set to
     */
    public PBool(PBool val) {
        value = val.value;
    }

    /**
     * Get the boolean value of the PBool.
     *
     * @return Boolean value
     */
    public boolean getValue() {
        return value;
    }

    /**
     * Logical Not operation
     * @return PBool object after operation
     */
    public PBool not() {
        return new PBool(!value);
    }

    /**
     * Logical And operation
     * @param val value to and to
     * @return PBool object after operation
     */
    public PBool and(PBool val) {
        return new PBool(value && val.value);
    }

    /**
     * Logical Or operation
     * @param val value to or to
     * @return PBool object after operation
     */
    public PBool or(PBool val) {
        return new PBool(value || val.value);
    }

    /**
     * Convert to a PInt
     * @return PInt object
     */
    public PInt toInt() {
        return new PInt(value?1:0);
    }

    @Override
    public PBool clone() {
        return new PBool(value);
    }

    @Override
    public int hashCode() {
        return Boolean.valueOf(value).hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof PBool)) {
            return false;
        }
        return this.value == ((PBool) obj).value;
    }

    @Override
    public String toString() {
        return Boolean.toString(value);
    }
}
