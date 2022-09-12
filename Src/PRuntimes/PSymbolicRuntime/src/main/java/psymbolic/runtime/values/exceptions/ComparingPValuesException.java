package psymbolic.runtime.values.exceptions;

import psymbolic.runtime.values.PValue;

public class ComparingPValuesException extends PRuntimeException {
    public ComparingPValuesException(String message) {
        super(message);
    }

    public ComparingPValuesException(PValue<?> one, PValue<?> two) {
        super(String.format("Invalid comparison: Comparing two incompatible values [%s] and [%s]", one.toString(), two.toString()));
    }
}