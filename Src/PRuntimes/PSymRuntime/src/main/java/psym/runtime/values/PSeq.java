package psym.runtime.values;

import java.util.ArrayList;
import java.util.List;
import psym.runtime.values.exceptions.InvalidIndexException;

public class PSeq extends PCollection {
  // stores the map
  private final List<PValue<?>> seq;

  public PSeq(List<PValue<?>> input_seq) {
    seq = new ArrayList<>();
    for (PValue<?> entry : input_seq) {
      seq.add(PValue.clone(entry));
    }
  }

  public PSeq(PSeq other) {
    seq = new ArrayList<>();
    for (PValue<?> entry : other.seq) {
      seq.add(PValue.clone(entry));
    }
  }

  public PValue<?> getValue(int index) throws InvalidIndexException {
    if (index >= seq.size() || index < 0) throw new InvalidIndexException(index, this);
    return seq.get(index);
  }

  public void setValue(int index, PValue<?> val) throws InvalidIndexException {
    if (index >= seq.size() || index < 0) throw new InvalidIndexException(index, this);
    seq.set(index, val);
  }

  public void insertValue(int index, PValue<?> val) throws InvalidIndexException {
    if (index > seq.size() || index < 0) throw new InvalidIndexException(index, this);
    seq.add(index, val);
  }

  @Override
  public PSeq clone() {
    return new PSeq(seq);
  }

  @Override
  public int hashCode() {
    return ComputeHash.getHashCode(seq);
  }

  @Override
  public boolean equals(Object obj) {
    if (obj == this) return true;

    if (!(obj instanceof PSeq)) {
      return false;
    }

    PSeq other = (PSeq) obj;
    if (seq.size() != other.seq.size()) {
      return false;
    }

    for (int i = 0; i < seq.size(); i++) {
      if (!PValue.equals(other.seq.get(i), this.seq.get(i))) {
        return false;
      }
    }
    return true;
  }

  @Override
  public String toString() {
    StringBuilder sb = new StringBuilder();
    sb.append("[");
    String sep = "";
    for (PValue<?> item : seq) {
      sb.append(sep);
      sb.append(item);
      sep = ", ";
    }
    sb.append("]");
    return sb.toString();
  }

  @Override
  public int size() {
    return seq.size();
  }

  @Override
  public boolean contains(PValue<?> item) {
    return seq.contains(item);
  }
}
