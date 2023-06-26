package psym.runtime.values;

import java.util.Arrays;
import psym.runtime.values.exceptions.TupleInvalidIndexException;

public class PTuple extends PValue<PTuple> {
  // stores the fields values
  private final PValue<?>[] fields;

  public PTuple(PValue<?>[] input_fields) {
    this.fields = new PValue<?>[input_fields.length];
    for (int i = 0; i < input_fields.length; i++) {
      this.fields[i] = PValue.clone(input_fields[i]);
    }
  }

  public PTuple(PTuple other) {
    this.fields = new PValue<?>[other.fields.length];
    for (int i = 0; i < other.fields.length; i++) {
      this.fields[i] = PValue.clone(other.fields[i]);
    }
  }

  public int getArity() {
    return fields.length;
  }

  public PValue<?> getField(int index) throws TupleInvalidIndexException {
    if (index >= fields.length) throw new TupleInvalidIndexException(this, index);
    return fields[index];
  }

  public void setField(int index, PValue<?> val) throws TupleInvalidIndexException {
    if (index >= fields.length) throw new TupleInvalidIndexException(this, index);
    fields[index] = val;
  }

  @Override
  public PTuple clone() {
    return new PTuple(fields);
  }

  @Override
  public int hashCode() {
    return Arrays.hashCode(fields);
  }

  @Override
  public boolean equals(Object obj) {
    if (obj == this) return true;

    if (!(obj instanceof PTuple)) {
      return false;
    }

    PTuple other = (PTuple) obj;
    if (fields.length != other.fields.length) {
      return false;
    }

    for (int i = 0; i < fields.length; i++) {
      if (!PValue.equals(fields[i], other.fields[i])) {
        return false;
      }
    }
    return true;
  }

  @Override
  public String toString() {
    StringBuilder sb = new StringBuilder();
    sb.append("(");
    String sep = "";
    for (PValue<?> field : fields) {
      sb.append(sep);
      sb.append(field);
      sep = ", ";
    }
    sb.append(")");
    return sb.toString();
  }
}
