package pcover.values;

import lombok.Getter;
import pcover.runtime.machine.PMachine;

/**
 * Represents the PValue for P machine
 */
public class PMachineValue extends PValue<PMachineValue> {
  @Getter private final PMachine value;

  /**
   * Constructor
   * @param val machine value to set to
   */
  public PMachineValue(PMachine val) {
    value = val;
  }

  /**
   * Constructor
   * @param val object from where value to set to
   */

  public PMachineValue(Object val) {
    if (val instanceof PMachineValue) value = ((PMachineValue) val).value;
    else value = (PMachine) val;
  }

  /**
   * Copy constructor
   * @param val value to copy from
   */
  public PMachineValue(PMachineValue val) {
    value = val.value;
  }

  /**
   * Get the unique machine identifier
   * @return unique machine instance id
   */
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
