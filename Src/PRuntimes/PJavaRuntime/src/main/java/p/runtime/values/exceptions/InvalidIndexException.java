package p.runtime.values.exceptions;

import p.runtime.PRuntimeException;
import p.runtime.values.PSeq;
import p.runtime.values.PSet;

public class InvalidIndexException extends PRuntimeException {
    public InvalidIndexException(String message) {
        super(message);
    }

    public InvalidIndexException(int index, PSeq seq)
    {
        super(String.format("Invalid index = %d into a Seq = %s. expected (0 <= index <= sizeof(seq)", index, seq.toString()));
    }

    public InvalidIndexException(int index, PSet set)
    {
        super(String.format("Invalid index = %d into a Set = %s. expected (0 <= index <= sizeof(set)", index, set.toString()));
    }
}
