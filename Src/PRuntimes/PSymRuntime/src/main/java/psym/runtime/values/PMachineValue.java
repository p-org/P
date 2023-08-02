package psym.runtime.values;

import psym.runtime.machine.Machine;

public class PMachineValue extends PValue<PMachineValue> {
  // stores the int value
  private final Machine value;

  public PMachineValue(Machine val) {
    value = val;
  }

  public PMachineValue(Object val) {
    if (val instanceof PMachineValue) value = ((PMachineValue) val).value;
    else value = (Machine) val;
  }

  public PMachineValue(PMachineValue val) {
    value = val.value;
  }

  public Machine getValue() {
    return value;
  }

  public int getId() {
    return value.getInstanceId();
  }

  @Override
  public PMachineValue clone() {
    return new PMachineValue(value);
  }

  @Override
  public int hashCode() {
    return value.hashCode();
  }

  @Override
  public boolean equals(Object obj) {
    if (obj == this) return true;
    else if (!(obj instanceof PMachineValue)) {
      return false;
    }
    return this.value.equals(((PMachineValue) obj).value);
  }

  @Override
  public String toString() {
    return value.toString();
  }
}
