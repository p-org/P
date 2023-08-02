package psym.runtime.values.exceptions;

import psym.runtime.values.PValue;

public class ComparingPValuesException extends PRuntimeException {

  public ComparingPValuesException(PValue<?> one, PValue<?> two) {
    super(
        String.format(
            "Invalid comparison: Comparing two incompatible values [%s] and [%s]",
            one.toString(), two.toString()));
  }
}
