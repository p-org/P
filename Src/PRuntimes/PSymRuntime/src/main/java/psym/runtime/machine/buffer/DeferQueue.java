package psym.runtime.machine.buffer;

import java.io.Serializable;
import psym.runtime.machine.events.Message;
import psym.valuesummary.Guard;

/** Implements the Defer Queue used to keep track of the deferred events */
public class DeferQueue extends SymbolicQueue implements Serializable {

  public DeferQueue() {
    super();
  }

  /**
   * Defer a particular event by adding it to the defer queue of the machine
   *
   * @param pc Guard under which to defer the event
   * @param event Event to be deferred
   */
  public void defer(Guard pc, Message event) {
    add(event.restrict(pc));
  }
}
