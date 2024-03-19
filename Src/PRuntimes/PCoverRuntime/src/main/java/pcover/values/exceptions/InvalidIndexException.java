package pcover.values.exceptions;

import pcover.utils.exceptions.PCoverRuntimeException;
import pcover.values.PSeq;
import pcover.values.PSet;

/**
 * Thrown when trying to index into a PSeq/PSet with an invalid index.
 */
public class InvalidIndexException extends PCoverRuntimeException {
    /**
     * Constructs a new InvalidIndexException with the specified message.
     */
    public InvalidIndexException(String message) {
        super(message);
    }

    /**
     * Constructs a new InvalidIndexException for PSeq
     */
    public InvalidIndexException(int index, PSeq seq) {
        super(
                String.format(
                        "Invalid index = %d into a Seq = %s. expected (0 <= index <= sizeof(seq)",
                        index, seq.toString()));
    }

    /**
     * Constructs a new InvalidIndexException for PSet
     */
    public InvalidIndexException(int index, PSet set) {
        super(
                String.format(
                        "Invalid index = %d into a Set = %s. expected (0 <= index <= sizeof(set)",
                        index, set.toString()));
    }
}
