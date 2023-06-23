package psym.runtime.values;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.stream.IntStream;
import psym.runtime.values.exceptions.InvalidIndexException;
import psym.runtime.values.exceptions.PRuntimeException;

public class PSet extends PCollection {
  // stores the map
  private final List<PValue<?>> set;

  public PSet(List<PValue<?>> input_set) {
    set = new ArrayList<>();
    for (PValue<?> entry : input_set) {
      set.add(PValue.clone(entry));
    }
    set.sort(new SortPValue());
  }

  public PSet(PSet other) {
    set = new ArrayList<>();
    for (PValue<?> entry : other.set) {
      set.add(PValue.clone(entry));
    }
    set.sort(new SortPValue());
  }

  public PValue<?> getValue(int index) throws InvalidIndexException {
    if (index >= set.size() || index < 0) throw new InvalidIndexException(index, this);
    return set.get(index);
  }

  public void setValue(int index, PValue<?> val) throws PRuntimeException {
    throw new PRuntimeException("Set value of a set is not allowed!");
  }

  public void insertValue(PValue<?> val) {
    set.add(val);
  }

  @Override
  public PSet clone() {
    return new PSet(set);
  }

  @Override
  public int hashCode() {
    return ComputeHash.getHashCode(set);
  }

  @Override
  public boolean equals(Object obj) {
    if (obj == this) return true;

    if (!(obj instanceof PSet)) {
      return false;
    }

    PSet other = (PSet) obj;
    if (set.size() != other.set.size()) {
      return false;
    }

    set.sort(new SortPValue());
    other.set.sort(new SortPValue());

    return IntStream.range(0, set.size())
        .allMatch(i -> PValue.equals(other.set.get(i), this.set.get(i)));
  }

  @Override
  public String toString() {
    StringBuilder sb = new StringBuilder();
    sb.append("(");
    String sep = "";
    for (PValue<?> item : set) {
      sb.append(sep);
      sb.append(item);
      sep = ", ";
    }
    sb.append(")");
    return sb.toString();
  }

  @Override
  public int size() {
    return set.size();
  }

  @Override
  public boolean contains(PValue<?> item) {
    return set.contains(item);
  }

  private static class SortPValue implements Comparator<PValue<?>> {
    @Override
    public int compare(PValue<?> o1, PValue<?> o2) {
      return o1.hashCode() - o2.hashCode();
    }
  }
}
