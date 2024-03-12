package pcover.runtime.machine.buffer;

import pcover.runtime.machine.Machine;
import pcover.values.PEvent;
import pcover.values.PValue;
import pcover.utils.exceptions.NotImplementedException;

import java.io.Serializable;

/**
 * Represents a FIFO event queue
 */
public class FifoQueue extends MessageQueue implements EventBuffer, Serializable {

  private final Machine sender;

  /**
   * Constructor
   * @param sender Sender machine (owner of the queue)
   */
  public FifoQueue(Machine sender) {
    super(sender);
    this.sender = sender;
  }

  /**
   * @inheritDoc
   */
  public void send(Machine target, PEvent eventName, PValue<?> payload) {
    throw new NotImplementedException();
  }

}
