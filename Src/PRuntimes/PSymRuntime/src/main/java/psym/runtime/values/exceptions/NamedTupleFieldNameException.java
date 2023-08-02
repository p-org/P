package psym.runtime.values.exceptions;

import psym.runtime.values.PNamedTuple;

/** Exception to capture the invalid field access for NamedTuples */
public class NamedTupleFieldNameException extends PRuntimeException {

  public NamedTupleFieldNameException(PNamedTuple tuple, String fieldName) {
    super(
        String.format(
            "Invalid field access: Trying to access field [%s] in the tuple [%s]",
            fieldName, tuple.toString()));
  }
}
