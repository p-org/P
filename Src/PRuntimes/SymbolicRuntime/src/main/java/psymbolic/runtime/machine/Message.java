package psymbolic.runtime.machine;


import psymbolic.runtime.Event;
import psymbolic.valuesummary.*;
import psymbolic.valuesummary.Guard;

import java.util.*;

/**
 * Represents a message in the sender buffer of a state machine
 */
public class Message implements ValueSummary<Message> {

    // the target machine to which the message is being sent
    private final PrimitiveVS<Machine> target;
    // the event sent to the target machine
    private final PrimitiveVS<Event> event;
    // the payload associated with the event
    private final Map<Event, UnionVS> payload;


    public PrimitiveVS<Boolean> canRun() {
        Guard cond = Guard.constFalse();
        for (GuardedValue<Machine> machine : getTarget().getGuardedValues()) {
            cond = cond.or(machine.getValue().hasStarted().getGuardFor(true).and(machine.getGuard()));

            if (BooleanVS.isEverFalse(machine.getValue().hasStarted())) {
                Guard unstarted = machine.getValue().hasStarted().getGuardFor(false).and(machine.getGuard());
                PrimitiveVS<Event> names = this.restrict(unstarted).getEvent();
                for (GuardedValue<Event> name : names.getGuardedValues()) {
                    if (name.getValue().equals(Event.Init)) {
                        cond = cond.or(name.getGuard());
                    }
                }
            }
        }
        return BooleanVS.trueUnderGuard(cond);
    }

    public PrimitiveVS<Boolean> isInit() {
        Guard cond = Guard.constFalse();
        for (GuardedValue<Machine> machine : getTarget().getGuardedValues()) {
            if (BooleanVS.isEverFalse(machine.getValue().hasStarted())) {
                Guard unstarted = machine.getValue().hasStarted().getGuardFor(false).and(machine.getGuard());
                PrimitiveVS<Event> events = this.restrict(unstarted).getEvent();
                for (GuardedValue<Event> event : events.getGuardedValues()) {
                    if (event.getValue().equals(Event.Init)) {
                        cond = cond.or(event.getGuard());
                    }
                }
            }
        }
        return BooleanVS.trueUnderGuard(cond);
    }

    public Message getForMachine(Machine machine) {
        Guard cond = this.target.getGuardFor(machine);
        return this.restrict(cond);
    }

    private Message(PrimitiveVS<Event> names, PrimitiveVS<Machine> machine, Map<Event, UnionVS> map) {
        this.event = names;
        this.target = machine;
        this.payload = new HashMap<>(map);
    }

    public Message(Event name, PrimitiveVS<Machine> machine) {
        this(new PrimitiveVS<>(name), machine, new HashMap<>());
    }

    public Message(PrimitiveVS<Event> name, PrimitiveVS<Machine> machine) {
        this(name, machine, new HashMap<>());
    }

    public Message() {
        this(new PrimitiveVS<>(), new PrimitiveVS<>());
    }

    public Message(Event name, PrimitiveVS<Machine> machine, UnionVS payload) {
        this(new PrimitiveVS<>(name), machine, payload);
    }

    public Message(PrimitiveVS<Event> events, PrimitiveVS<Machine> machine, UnionVS payload) {
        this.event = events;
        this.target = machine;
        this.payload = new HashMap<>();
        for (GuardedValue<Event> event : events.getGuardedValues()) {
            assert(!this.payload.containsKey(event.getValue()));
            if (payload != null) {
                this.payload.put(event.getValue(), payload.restrict(event.getGuard()));
            }
        }
    }

    public PrimitiveVS<Event> getEvent() {
        return this.event;
    }

    public PrimitiveVS<Machine> getTarget() {
        return this.target;
    }

    public UnionVS getPayload() {
        List<GuardedValue<Event>> names = this.event.getGuardedValues();
        assert(names.size() <= 1);
        if (names.size() == 0) {
            return null;
        } else {
            return payload.getOrDefault(names.get(0).getValue(), null);
        }
    }

    @Override
    public boolean isEmptyVS() {
        return event.isEmptyVS();
    }

    @Override
    public Message restrict(Guard guard) {
        Map<Event, UnionVS> newPayload = new HashMap<>();
        PrimitiveVS<Event> newEvent = event.restrict(guard);
        for (Map.Entry<Event, UnionVS> entry : payload.entrySet()) {
            if (!newEvent.getGuardFor(entry.getKey()).isFalse()) {
                newPayload.put(entry.getKey(), entry.getValue().restrict(guard));
            }
        }
        return new Message(newEvent, target.restrict(guard), newPayload);
    }

    @Override
    public Message merge(Iterable<Message> summaries) {
        List<PrimitiveVS<Event>> eventsToMerge = new ArrayList<>();
        List<PrimitiveVS<Machine>> targetsToMerge = new ArrayList<>();
        Map<Event, UnionVS> newPayload = new HashMap<>();

        for (Map.Entry<Event, UnionVS> entry : this.payload.entrySet()) {
            if (!event.getGuardFor(entry.getKey()).isFalse() && entry.getValue() != null) {
                newPayload.put(entry.getKey(), entry.getValue());
            }
        }

        for (Message summary : summaries) {
            eventsToMerge.add(summary.event);
            targetsToMerge.add(summary.target);
            for (Map.Entry<Event, UnionVS> entry : summary.payload.entrySet()) {
                newPayload.computeIfPresent(entry.getKey(), (key, value) -> value.merge(summary.payload.get(key)));
                if (entry.getValue() != null)
                    newPayload.putIfAbsent(entry.getKey(), entry.getValue());
                if (newPayload.containsKey(entry.getKey()) && newPayload.get(entry.getKey()) == null) {
                    assert(false);
                }
            }
        }

        return new Message(event.merge(eventsToMerge), target.merge(targetsToMerge), newPayload);
    }

    @Override
    public Message merge(Message summary) {
        assert (this.getUniverse().and(summary.getUniverse()).isFalse());
        return merge(Collections.singletonList(summary));
    }

    @Override
    public Message updateUnderGuard(Guard guard, Message update) {
        /*
        if (guard.isConstTrue()) {
            assert (this.guard(guard.not()).merge(update.guard(guard)).symbolicEquals(update, guard).getGuard(true).isConstTrue());
        }*/
        return this.restrict(guard.not()).merge(update.restrict(guard));
    }

    @Override
    public PrimitiveVS<Boolean> symbolicEquals(Message cmp, Guard pc) {
        Guard nameAndTarget = this.event.symbolicEquals(cmp.event, Guard.constTrue()).getGuardFor(true).and(
                                this.target.symbolicEquals(cmp.target, Guard.constTrue()).getGuardFor(true));
        Guard mapping = Guard.constFalse();
        for (GuardedValue<Event> event : event.getGuardedValues()) {
            if (this.payload.containsKey(event.getValue())) {
                if (cmp.payload.containsKey(event.getValue())) {
                    mapping = mapping.or(this.payload.get(event.getValue())
                                                  .symbolicEquals(cmp.payload.get(event.getValue()), Guard.constTrue())
                                                  .getGuardFor(true));
                }
            } else if (!cmp.payload.containsKey(event.getValue())) {
                mapping = mapping.or(event.getGuard());
            }
        }
        return BooleanVS.trueUnderGuard(pc.and(nameAndTarget).and(mapping));
    }

    @Override
    public Guard getUniverse() {
        return event.getUniverse();
    }

    @Override
    public String toString() {
        String str = "{";
        int i = 0;
        for (GuardedValue<Event> event : getEvent().getGuardedValues()) {
            //ScheduleLogger.log("name: " + name.value + " mach: " + this.guard(name.guard).getMachine());
            //if (getMachine().guard(name.guard).getGuardedValues().size() > 1) assert(false);
            str += event.getGuard();
            //str += " -> " + getMachine().guard(name.guard);
            if (payload.size() > 0 && payload.containsKey(event.getValue())) {
                str += ": " + payload.get(event.getValue());
            }
            if (i < getEvent().getGuardedValues().size() - 1)
                str += System.lineSeparator();
        }
        str += "}";
        return str;
    }

}
