package pex.values.exceptions;

import pex.utils.exceptions.BugFoundException;
import pex.values.PValue;

/**
 * Thrown when trying to compare two incompatible PValues.
 */
public class ComparingPValuesException extends BugFoundException {
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
