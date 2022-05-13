package psymbolic.runtime.machine.eventhandlers;

import lombok.Getter;
import psymbolic.runtime.Event;
import psymbolic.runtime.Message;
import psymbolic.runtime.machine.State;
import psymbolic.valuesummary.PrimitiveVS;
import psymbolic.valuesummary.UnionVS;
import psymbolic.valuesummary.Guard;

import java.io.Serializable;
import java.util.HashMap;
import java.util.Map;

/**
 * Represent the outcome of executing an event handler 
 * Either a normal return, or goto, or raise.
 */
public class EventHandlerReturnReason implements Serializable {

    private UnionVS outcome = new UnionVS();
    @Getter
    private Map<State, UnionVS> payloads = new HashMap<>();
    @Getter
    private Guard gotoCond = Guard.constFalse();
    @Getter
    private Guard raiseCond = Guard.constFalse();

    /**
     * Did the event handler terminated on a normal return without raise or goto statement
     * @return true if it was a normal return, false otherwise
     */
    public boolean isNormalReturn() {
        return gotoCond.or(raiseCond).isFalse();
    }


    /**
     * Condition under which the event handler did a goto
     * @return condition for goto
     */
    public Message getMessageSummary() { return (Message) outcome.getValue(Message.class).restrict(getRaiseCond()); }

    public void raiseGuardedMessage(Message newMessage) {
        outcome = outcome.merge(new UnionVS(newMessage));
        raiseCond = raiseCond.or(newMessage.getUniverse());
    }

    public void raiseGuardedEvent(Guard pc, PrimitiveVS<Event> event, UnionVS payload) {
        // TODO: Handle this in a more principled way
        if (event.getGuardedValues().size() != 1) {
            throw new RuntimeException("Raise statements with symbolically-determined event tags are not yet supported");
        }

        Event nextEvent = event.getValues().iterator().next();

        if (payload != null) payload = payload.restrict(pc);
        raiseGuardedMessage(new Message(nextEvent, new PrimitiveVS<>(), payload).restrict(pc));
    }

    public void raiseGuardedEvent(Guard pc, PrimitiveVS<Event> eventName) {
        raiseGuardedEvent(pc, eventName, null);
    }

    public PrimitiveVS<State> getGotoStateSummary() { return (PrimitiveVS<State>) outcome.getValue(PrimitiveVS.class).restrict(getGotoCond()); }

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
