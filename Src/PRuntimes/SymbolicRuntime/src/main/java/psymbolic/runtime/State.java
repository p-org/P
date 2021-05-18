package psymbolic.runtime;

import psymbolic.run.BugFoundException;
import psymbolic.util.Checks;
import psymbolic.valuesummary.*;
import psymbolic.valuesummary.bdd.Bdd;

import java.util.HashMap;
import java.util.Map;

public abstract class State extends HasId {
    private final Map<EventName, EventHandler> eventHandlers;

    public void entry(Bdd pc, Machine machine, Outcome outcome, UnionVS payload) {}
    public void exit(Bdd pc, Machine machine) {}

    public State(String name, int id, EventHandler... eventHandlers) {
        super(name, id);

        this.eventHandlers = new HashMap<>();
    }

    public void addHandlers(EventHandler... eventHandlers) {
        for (EventHandler handler : eventHandlers) {
            this.eventHandlers.put(handler.eventName, handler);
        }
    }

    public PrimVS<Boolean> hasHandler(Event event) {
        Bdd has = Bdd.constFalse();
        for (GuardedValue<EventName> entry : event.getName().getGuardedValues()) {
            if (eventHandlers.containsKey(entry.value)) {
                has = has.or(entry.guard);
            }
        }
        return BoolUtils.fromTrueGuard(has).guard(event.getUniverse());
    }

    public void handleEvent(Event event, Machine machine, Outcome outcome) {
        for (GuardedValue<EventName> entry : event.getName().getGuardedValues()) {
            EventName name = entry.value;
            Bdd eventPc = entry.guard;
            assert(event.guard(eventPc).getName().getGuardedValues().size() == 1);
            PrimVS<State> current = new PrimVS<>(this).guard(eventPc);
            ListVS<PrimVS<State>> stack = machine.getStack().guard(eventPc);
            ScheduleLogger.handle(machine,this, event.guard(entry.guard));
            Bdd handledPc = Bdd.constFalse();
            while (true) {
                for (GuardedValue<State> guardedValue : current.getGuardedValues()) {
                    if (guardedValue.value.eventHandlers.containsKey(name)) {
                        //System.out.println("payload: " + event.guard(guardedValue.guard).getPayload());
                        //if (event.guard(guardedValue.guard).getPayload() != null)
                            //System.out.println("payload class: " + event.guard(guardedValue.guard).getPayload().getClass());
                        machine.getScheduler().getSchedule().addTransition(machine.getScheduler().getDepth());
                        guardedValue.value.eventHandlers.get(name).handleEvent(
                                eventPc.and(guardedValue.guard),
                                event.guard(guardedValue.guard).getPayload(),
                                machine,
                                outcome
                        );
                        handledPc = handledPc.or(guardedValue.guard);
                    }
                }
                if (Checks.sameUniverse(handledPc, eventPc)) {
                    break; // handled the event along all paths
                } else {
                    stack = stack.guard(handledPc.not());
                    if (IntUtils.minValue(stack.size()) > 0) {
                        current = stack.get(IntUtils.subtract(stack.size(), 1));
                        stack = stack.removeAt(IntUtils.subtract(stack.size(), 1));
                    } else {
                        throw new BugFoundException("State " + this.name + " from machine " + machine + " missing handler for event: " + name, eventPc);
                    }
                }
            }
        }
    }

    @Override
    public String toString() {
        return String.format("%s#%d", name, id);
    }
}
