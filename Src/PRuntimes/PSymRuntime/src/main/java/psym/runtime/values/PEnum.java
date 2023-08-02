package psym.runtime.values;

public class PEnum extends PValue<PEnum> {
  // stores the int value

  private final String name;

  private final int value;

  public PEnum(String name, int val) {
    this.name = name;
    value = val;
  }

  public PEnum(PEnum val) {
    name = val.name;
    value = val.value;
  }

  public int getValue() {
    return value;
  }

  public String getName() {
    return name;
  }

  @Override
  public PEnum clone() {
    return new PEnum(this);
  }

  @Override
  public int hashCode() {
    return name.hashCode() ^ Long.hashCode(value);
  }

  @Override
  public boolean equals(Object obj) {
    if (obj == this) return true;
    else if (!(obj instanceof PEnum)) {
      return false;
    }
    return this.value == ((PEnum) obj).value && this.name.equals(((PEnum) obj).name);
  }

  @Override
  public String toString() {
    return name;
  }
}
