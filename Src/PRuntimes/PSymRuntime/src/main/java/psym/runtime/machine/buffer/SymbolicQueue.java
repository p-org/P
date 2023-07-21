package psym.runtime.machine.buffer;

import java.io.Serializable;
import java.util.function.Function;
import psym.runtime.PSymGlobal;
import psym.runtime.machine.Machine;
import psym.runtime.machine.events.Message;
import psym.runtime.scheduler.search.symmetry.SymmetryMode;
import psym.valuesummary.*;

/**
 * Represents a event-queue implementation using value summaries
 */
public abstract class SymbolicQueue implements Serializable {

  private final Machine owner;
  // elements in the queue
  protected ListVS<Message> elements;
  private Message peek = null;

  public SymbolicQueue(Machine m) {
    this.elements = new ListVS<>(Guard.constTrue());
    this.owner = m;
    assert (elements.getUniverse().isTrue());
  }

  public void resetPeek() {
    peek = null;
  }

  public PrimitiveVS<Integer> size() {
    return elements.size();
  }

  public PrimitiveVS<Integer> size(Guard pc) {
    return elements.restrict(pc).size();
  }

  public boolean isEmpty() {
    return elements.isEmpty();
  }

  public Guard isEnabledUnderGuard() {
    return elements.getNonEmptyUniverse();
  }

  public Message peek(Guard pc) {
    return peekOrDequeueHelper(pc, false);
  }

  private Message peekOrDequeueHelper(Guard pc, boolean dequeue) {
    boolean updatePeek = peek == null || !pc.implies(peek.getUniverse()).isTrue();
    if (!dequeue && !updatePeek) {
      return peek.restrict(pc);
    }
    assert (elements.getUniverse().isTrue());
    ListVS<Message> filtered = elements.restrict(pc);
    PrimitiveVS<Integer> idxVs = new PrimitiveVS<>(0, pc);
    Guard outOfRange = elements.inRange(idxVs).getGuardFor(false);
    if (!outOfRange.isFalse()) {
      throw new RuntimeException("Internal error: peeking into an empty queue");
    }
    if (updatePeek) {
      peek = filtered.get(idxVs);
    }
    Message ret = peek.restrict(pc);
    if (dequeue) {
      elements = elements.removeAt(idxVs);
      resetPeek();
    }
    assert (!pc.isFalse());
    return ret;
  }

  public void add(Message e) {
    if (PSymGlobal.getConfiguration().getSymmetryMode() != SymmetryMode.None) {
      PSymGlobal.getSymmetryTracker().updateSymmetrySet(owner, e.getUniverse());
    }
    elements = elements.add(e);
  }

  public PrimitiveVS<Boolean> satisfiesPredUnderGuard(
          Function<Message, PrimitiveVS<Boolean>> pred) {
    Guard cond = isEnabledUnderGuard();
    assert (!cond.isFalse());
    Message top = peek(cond);
    return pred.apply(top).restrict(top.getUniverse());
  }

  public Message remove(Guard pc) {
    if (PSymGlobal.getConfiguration().getSymmetryMode() != SymmetryMode.None) {
      PSymGlobal.getSymmetryTracker().updateSymmetrySet(owner, pc);
    }
    return peekOrDequeueHelper(pc, true);
  }

  public PrimitiveVS<Boolean> hasCreateMachineUnderGuard() {
    return satisfiesPredUnderGuard(Message::isCreateMachine);
  }

  public PrimitiveVS<Boolean> hasSyncEventUnderGuard() {
    return satisfiesPredUnderGuard(Message::isSyncEvent);
  }

  public ValueSummary getEvents() {
    return this.elements;
  }

  public void setEvents(ValueSummary events) {
    this.elements = (ListVS<Message>) events;
    resetPeek();
  }


  @Override
  public String toString() {
    return String.format("EventQueue{elements=%s}", elements);
  }
}
