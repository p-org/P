package psym.runtime.values;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import lombok.Getter;
import psym.runtime.values.exceptions.ComparingPValuesException;
import psym.runtime.values.exceptions.NamedTupleFieldNameException;

public class PNamedTuple extends PValue<PNamedTuple> {
  @Getter private final List<String> fields;
  // stores the mapping from field name to field value
  private final Map<String, PValue<?>> values;

  public PNamedTuple(List<String> input_fields, Map<String, PValue<?>> input_values) {
    fields = input_fields;
    values = new HashMap<>();
    for (Map.Entry<String, PValue<?>> entry : input_values.entrySet()) {
      values.put(entry.getKey(), PValue.clone(entry.getValue()));
    }
  }

  public PNamedTuple(PNamedTuple other) {
    fields = new ArrayList<>(other.getFields());
    values = new HashMap<>();
    for (Map.Entry<String, PValue<?>> entry : other.values.entrySet()) {
      values.put(entry.getKey(), PValue.clone(entry.getValue()));
    }
  }

  public PValue<?> getField(String name) throws NamedTupleFieldNameException {
    if (!values.containsKey(name)) throw new NamedTupleFieldNameException(this, name);
    return values.get(name);
  }

  public void setField(String name, PValue<?> val) throws NamedTupleFieldNameException {
    if (!values.containsKey(name)) throw new NamedTupleFieldNameException(this, name);
    values.put(name, val);
  }

  public PValue<?> getField(PString name) throws NamedTupleFieldNameException {
    return getField(name.toString());
  }

  public void setField(PString name, PValue<?> val) throws NamedTupleFieldNameException {
    setField(name.toString(), val);
  }

  @Override
  public PNamedTuple clone() {
    return new PNamedTuple(fields, values);
  }

  @Override
  public int hashCode() {
    return ComputeHash.getHashCode(values.values()) ^ ComputeHash.getHashCode(fields);
  }

  @Override
  public boolean equals(Object obj) {
    if (obj == this) return true;

    if (!(obj instanceof PNamedTuple)) {
      return false;
    }

    PNamedTuple other = (PNamedTuple) obj;
    if (fields.size() != other.fields.size()) {
      return false;
    }

    for (String name : fields) {
      if (!other.values.containsKey(name)) {
        throw new ComparingPValuesException(other, this);
      } else if (!PValue.equals(other.values.get(name), this.values.get(name))) {
        return false;
      }
    }
    return true;
  }

  @Override
  public String toString() {
    StringBuilder sb = new StringBuilder();
    sb.append("(");
    boolean hadElements = false;
    for (String name : fields) {
      if (hadElements) {
        sb.append(", ");
      }
      sb.append(name);
      sb.append(": ");
      sb.append(values.get(name));
      hadElements = true;
    }
    sb.append(")");
    return sb.toString();
  }
}
