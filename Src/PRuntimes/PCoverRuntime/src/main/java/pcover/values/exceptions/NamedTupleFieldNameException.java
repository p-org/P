package pcover.values.exceptions;

import pcover.utils.exceptions.PCoverRuntimeException;
import pcover.values.PNamedTuple;

/**
 * Exception to capture the invalid field access for NamedTuples
 */
public class NamedTupleFieldNameException extends PCoverRuntimeException {

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
