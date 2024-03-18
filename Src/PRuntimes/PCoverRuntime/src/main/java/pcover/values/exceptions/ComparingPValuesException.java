package pcover.values.exceptions;

import pcover.values.PValue;
import pcover.utils.exceptions.PCoverRuntimeException;

/**
 * Thrown when trying to compare two incompatible PValues.
 */
public class ComparingPValuesException extends PCoverRuntimeException {
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
