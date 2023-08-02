package psym.runtime.values;

public class PString extends PValue<PString> {
  // stores the int value
  private final String value;

  public PString(String val) {
    value = val;
  }

  public PString(Object val) {
    if (val instanceof PString) value = ((PString) val).value;
    else value = (String) val;
  }

  public PString(PString val) {
    value = val.value;
  }

  public String getValue() {
    return value;
  }

  @Override
  public PString clone() {
    return new PString(value);
  }

  @Override
  public int hashCode() {
    return value.hashCode();
  }

  @Override
  public boolean equals(Object obj) {
    if (obj == this) return true;
    else if (!(obj instanceof PString)) {
      return false;
    }
    return this.value.equals(((PString) obj).value);
  }

  @Override
  public String toString() {
    return value;
  }
}
