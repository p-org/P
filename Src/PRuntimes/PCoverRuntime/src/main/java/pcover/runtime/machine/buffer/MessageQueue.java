package pcover.runtime.machine.buffer;

import java.io.Serializable;
import java.util.List;
import java.util.ArrayList;

import lombok.Getter;
import pcover.runtime.machine.Machine;
import pcover.runtime.machine.events.Message;
import pcover.utils.exceptions.NotImplementedException;

/**
 * Represents an event queue
 */
public abstract class MessageQueue implements Serializable {

  private final Machine owner;
  @Getter protected List<Message> elements;
  private Message peek;

  /**
   * Constructor
   * @param owner Owner of the queue
   */
  public MessageQueue(Machine owner) {
    this.owner = owner;
    this.elements = new ArrayList<>();
    resetPeek();
  }

  /**
   * Reset the queue peek
   */
  public void resetPeek() {
    peek = null;
  }

  /**
   * Get the number of elements in the queue
   * @return Size of the queue
   */
  public int size() {
    return elements.size();
  }

  /**
   * Check whether or not the queue is empty
   * @return true if queue is empty, else false
   */
  public boolean isEmpty() {
    return elements.isEmpty();
  }

  /**
   * Get the peek message in the queue
   * @return Peek message in the queue
   */
  public Message peek() {
    return peekOrDequeueHelper(false);
  }

  /**
   * TODO
   * Get (or dequeue) the next message in the queue
   * @param dequeue Whether or not to dequeue the message from the queue
   * @return The next message in the queue, or null if queue is empty
   */
  private Message peekOrDequeueHelper(boolean dequeue) {
    throw new NotImplementedException();
  }

  /**
   * TODO
   * @param e
   */
  public void add(Message e) {
    throw new NotImplementedException();
  }

  /**
   * TODO
   * @return
   */
  public Message remove() {
    throw new NotImplementedException();
  }

  /**
   * Set the queue elements based on the input messages
   * @param messages Input messages
   */
  public void setElements(List<Message> messages) {
    this.elements = messages;
    resetPeek();
  }

  @Override
  public String toString() {
    return String.format("MessageQueue{elements=%s}", elements);
  }
}
