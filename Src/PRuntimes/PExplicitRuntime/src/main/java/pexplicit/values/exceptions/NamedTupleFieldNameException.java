package pexplicit.values.exceptions;

import pexplicit.utils.exceptions.BugFoundException;
import pexplicit.utils.exceptions.PExplicitRuntimeException;
import pexplicit.values.PNamedTuple;

/**
 * Exception to capture the invalid field access for NamedTuples
 */
public class NamedTupleFieldNameException extends BugFoundException {

    /**
     * Constructor for the exception
     */
    public NamedTupleFieldNameException(PNamedTuple tuple, String fieldName) {
        super(
                String.format(
                        "Invalid field access: Trying to access field [%s] in the tuple [%s]",
                        fieldName, tuple.toString()));
    }
}
