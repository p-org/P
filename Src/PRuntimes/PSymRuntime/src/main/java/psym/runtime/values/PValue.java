package psym.runtime.values;

/**
 * Interface that must be implemented by all P values.
 *
 * @param <T> T is the type of P Value
 */
public abstract class PValue<T extends PValue<T>> {
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
    static boolean equals(PValue<?> val1, PValue<?> val2) {
        if (val1 == null) {
            return val2 == null;
        }
        return val1.equals(val2);
    }

    /**
     * Function to create a deep clone of the PValue
     *
     * @return deep clone of the PValue
     */
    public abstract T clone();

    /**
     * Returns the hash code for the PValue
     *
     * @return hash code
     */
    public abstract int hashCode();

    /**
     * Checks if the current PValue is equal to the passed PValue
     *
     * @param other the other value
     * @return true if the two values are equal and false otherwise
     */
    public abstract boolean equals(Object other);

    public abstract String toString();

}
