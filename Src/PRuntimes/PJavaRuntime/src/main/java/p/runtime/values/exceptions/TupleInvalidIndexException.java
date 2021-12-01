package p.runtime.values.exceptions;

import p.runtime.PRuntimeException;
import p.runtime.values.PTuple;

public class TupleInvalidIndexException extends PRuntimeException {
    public TupleInvalidIndexException(String message) {
        super(message);
    }

    public TupleInvalidIndexException(PTuple tuple, int fieldIndex) {
        super(String.format("Invalid field access: Trying to access field at index [%d] in the tuple [%s]", fieldIndex, tuple.toString()));
    }
}
