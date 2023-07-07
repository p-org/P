package psym.runtime.machine.eventhandlers;

import java.io.Serializable;
import java.util.HashMap;
import java.util.Map;
import lombok.Getter;
import psym.runtime.machine.State;
import psym.runtime.machine.events.Event;
import psym.runtime.machine.events.Message;
import psym.valuesummary.Guard;
import psym.valuesummary.PrimitiveVS;
import psym.valuesummary.UnionVS;
import psym.valuesummary.UnionVStype;

/**
 * Represent the outcome of executing an event handler Either a normal return, or goto, or raise.
 */
public class EventHandlerReturnReason implements Serializable {

  @Getter private final Map<State, UnionVS> payloads = new HashMap<>();
  private UnionVS outcome = new UnionVS();
  @Getter private Guard gotoCond = Guard.constFalse();
  @Getter private Guard raiseCond = Guard.constFalse();

  /**
   * Did the event handler terminated on a normal return without raise or goto statement
   *
   * @return true if it was a normal return, false otherwise
   */
  public boolean isAbnormalReturn() {
    return !gotoCond.or(raiseCond).isFalse();
  }

  /**
   * Condition under which the event handler did a goto
   *
   * @return condition for goto
   */
  public Message getMessageSummary() {
    return (Message)
        outcome.getValue(UnionVStype.getUnionVStype(Message.class, null)).restrict(getRaiseCond());
  }

  public void raiseGuardedMessage(Message newMessage) {
    outcome = outcome.merge(new UnionVS(newMessage));
    raiseCond = raiseCond.or(newMessage.getUniverse());
  }

  public void raiseGuardedEvent(Guard pc, PrimitiveVS<Event> event, UnionVS payload) {
    // TODO: Handle this in a more principled way
    if (event.getGuardedValues().size() != 1) {
      throw new RuntimeException(
          "Raise statements with symbolically-determined event tags are not yet supported");
    }

    Event nextEvent = event.getValues().iterator().next();

    if (payload != null) payload = payload.restrict(pc);
    raiseGuardedMessage(new Message(nextEvent, new PrimitiveVS<>(), payload).restrict(pc));
  }

  public void raiseGuardedEvent(Guard pc, PrimitiveVS<Event> eventName) {
    raiseGuardedEvent(pc, eventName, null);
  }

  public PrimitiveVS<State> getGotoStateSummary() {
    return (PrimitiveVS<State>)
        outcome
            .getValue(UnionVStype.getUnionVStype(PrimitiveVS.class, null))
            .restrict(getGotoCond());
  }

  public void addGuardedGoto(Guard pc, State newDest, UnionVS newPayload) {
    outcome = outcome.merge(new UnionVS(new PrimitiveVS<>(newDest).restrict(pc)));
    if (newPayload != null) {
      payloads.merge(newDest, newPayload, UnionVS::merge);
    }
    gotoCond = gotoCond.or(pc);
  }

  public void addGuardedGoto(Guard pc, State newDest) {
    addGuardedGoto(pc, newDest, null);
  }
}
