package pexplicit.values.exceptions;

import pexplicit.utils.exceptions.PExplicitRuntimeException;
import pexplicit.values.PValue;

/**
 * Thrown when trying to compare two incompatible PValues.
 */
public class ComparingPValuesException extends PExplicitRuntimeException {
    /**
     * Constructor.
     */
    public ComparingPValuesException(PValue<?> one, PValue<?> two) {
        super(
                String.format(
                        "Invalid comparison: Comparing two incompatible values [%s] and [%s]",
                        one.toString(), two.toString()));
    }
}
