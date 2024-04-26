package pexplicit.values;

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
        initialize();
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
        initialize();
    }

    /**
     * Copy constructor
     *
     * @param val Value to set to
     */
    public PFloat(PFloat val) {
        value = val.value;
        initialize();
    }

    /**
     * Negation operation
     *
     * @return Result after operation
     */
    public PFloat negate() {
        return new PFloat(-value);
    }

    /**
     * Add operation
     *
     * @param val value to add
     * @return Result after addition
     */
    public PFloat add(PFloat val) {
        return new PFloat(value + val.value);
    }

    /**
     * Subtract operation
     *
     * @param val value to subtract
     * @return Result after subtraction
     */
    public PFloat sub(PFloat val) {
        return new PFloat(value - val.value);
    }

    /**
     * Multiply operation
     *
     * @param val value to multiply
     * @return Result after multiplication
     */
    public PFloat mul(PFloat val) {
        return new PFloat(value * val.value);
    }

    /**
     * Divide operation
     *
     * @param val value to divide
     * @return Result after division
     */
    public PFloat div(PFloat val) {
        return new PFloat(value / val.value);
    }

    /**
     * Modulo operation
     *
     * @param val value to modulo
     * @return Result after modulo
     */
    public PFloat mod(PFloat val) {
        return new PFloat(value % val.value);
    }

    /**
     * Less than operation
     *
     * @param val value to compare to
     * @return PBool object after operation
     */
    public PBool lt(PFloat val) {
        return new PBool(value < val.value);
    }

    /**
     * Less than or equal to operation
     *
     * @param val value to compare to
     * @return PBool object after operation
     */
    public PBool le(PFloat val) {
        return new PBool(value <= val.value);
    }

    /**
     * Greater than operation
     *
     * @param val value to compare to
     * @return PBool object after operation
     */
    public PBool gt(PFloat val) {
        return new PBool(value > val.value);
    }

    /**
     * Greater than or equal to operation
     *
     * @param val value to compare to
     * @return PBool object after operation
     */
    public PBool ge(PFloat val) {
        return new PBool(value >= val.value);
    }

    /**
     * Convert to a PInt
     *
     * @return PInt object
     */
    public PInt toInt() {
        return new PInt((int) value);
    }


    @Override
    public PFloat clone() {
        return new PFloat(value);
    }

    @Override
    protected String _asString() {
        return Double.toString(value);
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
}
