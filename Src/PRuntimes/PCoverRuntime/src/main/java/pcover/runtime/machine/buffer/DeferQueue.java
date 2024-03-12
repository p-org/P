package pcover.runtime.machine.buffer;

import java.io.Serializable;
import pcover.runtime.machine.Machine;

/**
 * Implements the defer queue used to keep track of the deferred events.
 */
public class DeferQueue extends MessageQueue implements Serializable {

  /**
   * Constructor
   * @param owner Owner machine
   */
  public DeferQueue(Machine owner) {
    super(owner);
  }
}
