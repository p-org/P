package pexplicit.values.exceptions;

import pexplicit.utils.exceptions.BugFoundException;
import pexplicit.utils.exceptions.PExplicitRuntimeException;
import pexplicit.values.PTuple;

/**
 * Thrown when trying to access a field at an invalid index in a tuple.
 */
public class TupleInvalidIndexException extends BugFoundException {

    /**
     * Constructor.
     */
    public TupleInvalidIndexException(PTuple tuple, int fieldIndex) {
        super(String.format("Invalid field access: Trying to access field at index [%d] in the tuple [%s]", fieldIndex, tuple.toString()));
    }
}
