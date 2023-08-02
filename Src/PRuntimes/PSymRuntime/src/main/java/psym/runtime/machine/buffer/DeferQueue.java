package psym.runtime.machine.buffer;

import java.io.Serializable;
import psym.runtime.machine.Machine;

/** Implements the Defer Queue used to keep track of the deferred events */
public class DeferQueue extends SymbolicQueue implements Serializable {

  public DeferQueue(Machine owner) {
    super(owner);
  }
}
