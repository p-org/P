package psymbolic.runtime.concretevalues.exceptions;

import psymbolic.runtime.concretevalues.PRuntimeException;
import psymbolic.runtime.concretevalues.PNamedTuple;

/**
 * Exception to capture the invalid field access for NamedTuples
 */
public class NamedTupleFieldNameException extends PRuntimeException {
    public NamedTupleFieldNameException(String message) {
        super(message);
    }

    public NamedTupleFieldNameException(PNamedTuple tuple, String fieldName) {
        super(String.format("Invalid field access: Trying to access field [%s] in the tuple [%s]", fieldName, tuple.toString()));
    }
}
