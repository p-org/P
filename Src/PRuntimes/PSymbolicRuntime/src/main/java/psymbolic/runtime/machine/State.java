package psymbolic.runtime.machine;

import psymbolic.commandline.BugFoundException;
import psymbolic.runtime.Event;
import psymbolic.runtime.Message;
import psymbolic.runtime.logger.TraceLogger;
import psymbolic.runtime.machine.eventhandlers.EventHandler;
import psymbolic.runtime.machine.eventhandlers.IgnoreEventHandler;
import psymbolic.runtime.machine.eventhandlers.EventHandlerReturnReason;
import psymbolic.valuesummary.*;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.util.ValueSummaryChecks;

import java.io.Serializable;
import java.util.HashMap;
import java.util.Map;
import java.util.List;
import java.util.ArrayList;

public abstract class State implements Serializable {
    private final Map<Event, EventHandler> eventHandlers;
    private final String name;
    private final List<Event> ignored;
    public void entry(Guard pc, Machine machine, EventHandlerReturnReason outcome, UnionVS payload) {}
    public void exit(Guard pc, Machine machine) {}

    public State(String name, EventHandler... eventHandlers) {
        this.eventHandlers = new HashMap<>();
        this.name = name;
        this.ignored = new ArrayList<>();
    }

    public void addHandlers(EventHandler... eventHandlers) {
        for (EventHandler handler : eventHandlers) {
            this.eventHandlers.put(handler.event, handler);
            if (handler instanceof IgnoreEventHandler) {
                ignored.add(handler.event);
            }
        }
    }

    public Boolean isIgnored(Event event) {
        return ignored.contains(event);
    }

    public PrimitiveVS<Boolean> hasHandler(Message message) {
        Guard has = Guard.constFalse();
        for (GuardedValue<Event> entry : message.getEvent().getGuardedValues()) {
            if (eventHandlers.containsKey(entry.getValue())) {
                has = has.or(entry.getGuard());
            }
        }
        return BooleanVS.trueUnderGuard(has).restrict(message.getUniverse());
    }

    public void handleEvent(Message message, Machine machine, EventHandlerReturnReason outcome) {
        for (GuardedValue<Event> entry : message.getEvent().getGuardedValues()) {
            Event event = entry.getValue();
            Guard eventPc = entry.getGuard();
            assert (message.restrict(eventPc).getEvent().getGuardedValues().size() == 1);
            PrimitiveVS<State> current = new PrimitiveVS<>(this).restrict(eventPc);
            TraceLogger.handle(machine, this, message.restrict(entry.getGuard()));
            Guard handledPc = Guard.constFalse();
            for (GuardedValue<State> guardedValue : current.getGuardedValues()) {
                if (guardedValue.getValue().eventHandlers.containsKey(event)) {
                    //System.out.println("payload: " + event.guard(guardedValue.guard).getPayload());
                    //if (event.guard(guardedValue.guard).getPayload() != null)
                    //System.out.println("payload class: " + event.guard(guardedValue.guard).getPayload().getClass());
                    guardedValue.getValue().eventHandlers.get(event).handleEvent(
                            eventPc.and(guardedValue.getGuard()),
                            machine,
                            message.restrict(guardedValue.getGuard()).getPayload(),
                            outcome
                    );
                    handledPc = handledPc.or(guardedValue.getGuard());
                }
            }
            if (event.equals(Event.haltMachine)) {
                machine.halt(eventPc.and(handledPc.not()));
            }
            else if (!ValueSummaryChecks.hasSameUniverse(handledPc, eventPc)) {
                new BugFoundException("State " + this.name + " missing handler for event: " + event, eventPc);
            }
        }
    }

    @Override
    public String toString() {
        return String.format("%s", name);
    }
}
