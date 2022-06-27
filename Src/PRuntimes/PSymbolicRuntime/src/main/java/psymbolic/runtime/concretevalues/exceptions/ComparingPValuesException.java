package psymbolic.runtime.concretevalues.exceptions;

import psymbolic.runtime.concretevalues.PRuntimeException;
import psymbolic.runtime.concretevalues.PValue;

public class ComparingPValuesException extends PRuntimeException {
    public ComparingPValuesException(String message) {
        super(message);
    }

    public ComparingPValuesException(PValue<?> one, PValue<?> two) {
        super(String.format("Invalid comparison: Comparing two incompatible concretevalues [%s] and [%s]", one.toString(), two.toString()));
    }
}