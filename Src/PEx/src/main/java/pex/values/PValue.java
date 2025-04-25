package pex.values;

import java.io.Serializable;
import java.util.Objects;

/**
 * Interface that must be implemented by all P data types.
 *
 * @param <T> T is the type of P Value
 */
public abstract class PValue<T extends PValue<T>> implements Serializable {
    /**
     * Hash code corresponding to the PValue
     */
    private int hashCode;
    /**
     * String representation of the PValue
     */
    private String stringRep;

    /**
     * Create a safe clone of the passed PValue
     *
     * @param value Value to be cloned
     * @param <S>   Type of the PValue to be cloned
     * @return A deep clone of the passed PValue
     */
    static <S extends PValue<S>> S clone(PValue<S> value) {
        S result = null;
        if (value != null) {
            result = value.clone();
        }
        return result;
    }

    /**
     * Checks the equality of two PValues
     *
     * @param val1 first PValue
     * @param val2 second PValue
     * @return true if the two values are equal and false otherwise
     */
    public static boolean isEqual(PValue<?> val1, PValue<?> val2) {
        if (val1 == null) {
            return val2 == null;
        }
        return val1.equals(val2);
    }

    /**
     * Checks whether two PValues are not equal
     *
     * @param val1 first PValue
     * @param val2 second PValue
     * @return true if the two values are not equal and false otherwise
     */
    public static boolean notEqual(PValue<?> val1, PValue<?> val2) {
        return !isEqual(val1, val2);
    }

    protected void initialize() {
        stringRep = _asString();
        hashCode = Objects.hashCode(stringRep);
    }

    /**
     * Returns the hash code for the PValue
     *
     * @return hash code
     */
    @Override
    public int hashCode() {
        return hashCode;
    }

    /**
     * Returns the string representation of the PValue
     *
     * @return string representation of the PValue
     */
    @Override
    public String toString() {
        return stringRep;
    }

    /**
     * Computes the string representation for the PValue
     */
    protected abstract String _asString();

    /**
     * Function to create a default value of the PValue
     *
     * @return default value
     */
    public abstract T getDefault();

    /**
     * Function to create a deep clone of the PValue
     *
     * @return deep clone of the PValue
     */
    public abstract T clone();

    /**
     * Checks if the current PValue is equal to the passed PValue
     *
     * @param other the other value
     * @return true if the two values are equal and false otherwise
     */
    public abstract boolean equals(Object other);

}
