package pcover.runtime.machine.buffer;

import pcover.runtime.machine.PMachine;
import pcover.values.PEvent;
import pcover.values.PValue;
import pcover.utils.exceptions.NotImplementedException;

import java.io.Serializable;

/**
 * Represents a FIFO event queue
 */
public class FifoQueue extends MessageQueue implements EventBuffer, Serializable {

  private final PMachine sender;

  /**
   * Constructor
   * @param sender Sender machine (owner of the queue)
   */
  public FifoQueue(PMachine sender) {
    super(sender);
    this.sender = sender;
  }

  /**
   * @inheritDoc
   */
  public void send(PMachine target, PEvent eventName, PValue<?> payload) {
    throw new NotImplementedException();
  }

}
