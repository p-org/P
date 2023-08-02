package psym.runtime.values.exceptions;

import psym.runtime.values.PSeq;
import psym.runtime.values.PSet;

public class InvalidIndexException extends PRuntimeException {
  public InvalidIndexException(String message) {
    super(message);
  }

  public InvalidIndexException(int index, PSeq seq) {
    super(
        String.format(
            "Invalid index = %d into a Seq = %s. expected (0 <= index <= sizeof(seq)",
            index, seq.toString()));
  }

  public InvalidIndexException(int index, PSet set) {
    super(
        String.format(
            "Invalid index = %d into a Set = %s. expected (0 <= index <= sizeof(set)",
            index, set.toString()));
  }
}
