package pexplicit.runtime.machine.buffer;

import lombok.Getter;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.events.PMessage;
import pexplicit.utils.exceptions.NotImplementedException;
import pexplicit.utils.misc.Assert;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.List;
import java.util.function.Function;

/**
 * Represents an event queue
 */
public abstract class MessageQueue implements Serializable {

    private final PMachine owner;
    @Getter
    protected List<PMessage> elements;
    private int peekIdx;

    /**
     * Constructor
     *
     * @param owner Owner of the queue
     */
    public MessageQueue(PMachine owner) {
        this.owner = owner;
        this.elements = new ArrayList<>();
        resetPeek();
    }

    /**
     * Set the peek message index in the queue
     *
     * @param idx Index to set as peek
     */
    private void setPeek(int idx) {
        peekIdx = idx;
    }

    /**
     * Reset the queue peek
     */
    public void resetPeek() {
        setPeek(-1);
    }

    /**
     * Check whether the peek is valid
     * @return true if peek is valid, else false
     */
    private boolean isPeekValid() {
        if (peekIdx == -1) {
            return false;
        } else {
            assert (peekIdx >= 0 && peekIdx <= elements.size());
            return true;
        }
    }

    /**
     * Get the peek message corresponding to the peek index
     * Assumes peek index is already valid
     * @return peek message corresponding to the peek index
     */
    private PMessage getPeekMsg() {
        assert (isPeekValid());
        if (peekIdx < elements.size()) {
            return elements.get(peekIdx);
        } else {
            return null;
        }
    }

    /**
     * Get the number of elements in the queue
     *
     * @return Size of the queue
     */
    public int size() {
        return elements.size();
    }

    /**
     * Check whether or not the queue is empty
     *
     * @return true if queue is empty, else false
     */
    public boolean isEmpty() {
        return elements.isEmpty();
    }

    /**
     * Get the peek message in the queue
     *
     * @return Peek message in the queue
     */
    public PMessage peek() {
        return peekOrDequeueHelper(false);
    }

    /**
     * Peek (or dequeue) the next non-deferred message in the queue
     * Utilizes valid peek to improve performance and resetting peek only on state transitions
     *
     * @param dequeue Whether to dequeue the message from the queue
     * @return The next message in the queue, or null if queue is empty
     */
    private PMessage peekOrDequeueHelper(boolean dequeue) {
        boolean validPeek = isPeekValid();

        if (!dequeue && validPeek) {
            // just peeking and peek is valid
            return getPeekMsg();
        }

        PMessage msg = null;
        int msgIdx = 0;

        if (validPeek) {
            // peek is valid, so we can use it
            msgIdx = peekIdx;
            msg = getPeekMsg();
        } else {
            // peek is not valid, so we need to find the first non-deferred message

            // find the first non-deferred message
            for (PMessage m: elements) {
                if (!owner.getCurrentState().isDeferred(m.getEvent())) {
                    msg = m;
                    break;
                }
                msgIdx++;
            }

            // update peek
            setPeek(msgIdx);
        }

        // dequeue the peek
        if (dequeue) {
            if (msg == null) {
                if (elements.isEmpty()) {
                    Assert.prop(false, "Cannot dequeue from empty queue");
                } else {
                    Assert.prop(false, "Cannot dequeue since all events in the queue are deferred");
                }
            } else {
                elements.remove(msgIdx);
                if (msgIdx < elements.size()) {
                    // add the next element as peek
                    setPeek(msgIdx);
                } else {
                    // no next element, reset peek
                    resetPeek();
                }
            }
        }

        return msg;
    }

    /**
     * Add a message to the queue.
     *
     * @param msg Message to add
     */
    public void add(PMessage msg) {
        elements.add(msg);
    }

    /**
     * Pop a non-deferred event from the queue
     *
     * @return Next non-deferred event in the queue, or null if no such event exists.
     */
    public PMessage remove() {
        return peekOrDequeueHelper(true);
    }

    /**
     * Clear the queue.
     */
    public void clear() {
        elements.clear();
        resetPeek();
    }

    /**
     * Set the queue elements based on the input messages
     *
     * @param messages Input messages
     */
    public void setElements(List<PMessage> messages) {
        this.elements = messages;
        resetPeek();
    }

    /**
     * Check if the next message in the queue satisfies the predicate
     *
     * @param pred Predicate to check
     * @return true if next message satisfies the predicate, else false
     */
    public boolean nextSatisfiesPred(Function<PMessage, Boolean> pred) {
        PMessage next = peek();
        if (next != null) {
            return pred.apply(next);
        }
        return false;
    }

    /**
     * Check if the next message in the queue is a create machine message
     *
     * @return true if next message is a create machine message, else false
     */
    public boolean nextIsCreateMachineMsg() {
        return nextSatisfiesPred(x -> x.getEvent().isCreateMachineEvent());
    }

    /**
     * Check if the next message in the queue has the target machine running
     *
     * @return true if next message has target machine running, else false
     */
    public boolean nextHasTargetRunning() {
        return nextSatisfiesPred(x -> x.getTarget().canRun());
    }

    @Override
    public String toString() {
        return String.format("MessageQueue{elements=%s}", elements);
    }
}
