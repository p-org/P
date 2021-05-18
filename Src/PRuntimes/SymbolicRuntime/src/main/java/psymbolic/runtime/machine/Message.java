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
        for (GuardedValue<Machine> machine : getMachine().getGuardedValues()) {
            cond = cond.or(machine.getValue().hasStarted().getGuard(true).and(machine.getGuard()));

            if (BoolUtils.isEverFalse(machine.value.hasStarted())) {
                Guard unstarted = machine.value.hasStarted().getGuard(false).and(machine.guard);
                PrimitiveVS<EventName> names = this.guard(unstarted).getName();
                for (GuardedValue<EventName> name : names.getGuardedValues()) {
                    if (name.value.equals(EventName.Init.instance)) {
                        cond = cond.or(name.guard);
                    }
                }
            }
        }
        return BoolUtils.fromTrueGuard(cond);
    }

    public PrimitiveVS<Boolean> isInit() {
        Guard cond = Guard.constFalse();
        for (GuardedValue<Machine> machine : getMachine().getGuardedValues()) {
            if (BoolUtils.isEverFalse(machine.value.hasStarted())) {
                Guard unstarted = machine.value.hasStarted().getGuard(false).and(machine.guard);
                PrimitiveVS<EventName> names = this.guard(unstarted).getName();
                for (GuardedValue<EventName> name : names.getGuardedValues()) {
                    if (name.value.equals(EventName.Init.instance)) {
                        cond = cond.or(name.guard);
                    }
                }
            }
        }
        return BoolUtils.fromTrueGuard(cond);
    }

    public Message getForMachine(Machine machine) {
        Guard cond = this.machine.getGuard(machine);
        return this.guard(cond);
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

    public Message(PrimitiveVS<Event> names, PrimitiveVS<Machine> machine, UnionVS payload) {
        this.name = names;
        this.machine = machine;
        this.map = new HashMap<>();
        this.clock = clock;
        for (GuardedValue<EventName> name : names.getGuardedValues()) {
            assert(!this.map.containsKey(name));
            if (payload != null) {
                this.map.put(name.value, payload.guard(name.guard));
            }
        }
    }

    public PrimitiveVS<EventName> getName() {
        return this.name;
    }

    public PrimitiveVS<Machine> getMachine() {
        return this.machine;
    }

    public UnionVS getPayload() {
        List<GuardedValue<EventName>> names = this.name.getGuardedValues();
        assert(names.size() <= 1);
        if (names.size() == 0) {
            return null;
        } else {
            return map.getOrDefault(names.get(0).value, null);
        }
    }

    public VectorClockVS getVectorClock() {
        return this.clock;
    }

    @Override
    public boolean isEmptyVS() {
        return name.isEmptyVS();
    }

    @Override
    public Message guard(Guard guard) {
        Map<EventName, UnionVS> newMap = new HashMap<>();
        PrimitiveVS<EventName> newName = name.guard(guard);
        for (Map.Entry<EventName, UnionVS> entry : map.entrySet()) {
            if (!newName.getGuard(entry.getKey()).isConstFalse()) {
                newMap.put(entry.getKey(), entry.getValue().guard(guard));
            }
        }
        return new Message(name.guard(guard), clock.guard(guard), machine.guard(guard), newMap);
    }

    @Override
    public Message merge(Iterable<Message> summaries) {
        List<PrimitiveVS<EventName>> namesToMerge = new ArrayList<>();
        List<VectorClockVS> clocksToMerge = new ArrayList<>();
        List<PrimitiveVS<Machine>> machinesToMerge = new ArrayList<>();
        Map<EventName, UnionVS> newMap = new HashMap<>();

        for (Map.Entry<EventName, UnionVS> entry : this.map.entrySet()) {
            if (!name.getGuard(entry.getKey()).isConstFalse() && entry.getValue() != null) {
                newMap.put(entry.getKey(), entry.getValue());
            }
        }

        for (Message summary : summaries) {
            namesToMerge.add(summary.name);
            clocksToMerge.add(summary.clock);
            machinesToMerge.add(summary.machine);
            for (Map.Entry<EventName, UnionVS> entry : summary.map.entrySet()) {
                newMap.computeIfPresent(entry.getKey(), (key, value) -> value.merge(summary.map.get(key)));
                if (entry.getValue() != null)
                    newMap.putIfAbsent(entry.getKey(), entry.getValue());
                if (newMap.containsKey(entry.getKey()) && newMap.get(entry.getKey()) == null) {
                    assert(false);
                }
            }
        }

        return new Message(name.merge(namesToMerge), clock.merge(clocksToMerge), machine.merge(machinesToMerge), newMap);
    }

    @Override
    public Message merge(Message summary) {
        assert (this.getUniverse().and(summary.getUniverse()).isConstFalse());
        return merge(Collections.singletonList(summary));
    }

    @Override
    public Message update(Guard guard, Message update) {
        /*
        if (guard.isConstTrue()) {
            assert (this.guard(guard.not()).merge(update.guard(guard)).symbolicEquals(update, guard).getGuard(true).isConstTrue());
        }*/
        return this.guard(guard.not()).merge(update.guard(guard));
    }

    @Override
    public PrimitiveVS<Boolean> symbolicEquals(Message cmp, Guard pc) {
        Guard nameAndTarget = this.name.symbolicEquals(cmp.name, Guard.constTrue()).getGuard(true).and(
                                this.machine.symbolicEquals(cmp.machine, Guard.constTrue()).getGuard(true));
        Guard mapping = Guard.constFalse();
        for (GuardedValue<EventName> event : name.getGuardedValues()) {
            if (this.map.containsKey(event.value)) {
                if (cmp.map.containsKey(event.value)) {
                    mapping = mapping.or(this.map.get(event.value)
                                                  .symbolicEquals(cmp.map.get(event.value), Guard.constTrue())
                                                  .getGuard(true));
                }
            } else if (!cmp.map.containsKey(event.value)) {
                mapping = mapping.or(event.guard);
            }
        }
        return BoolUtils.fromTrueGuard(pc.and(nameAndTarget).and(mapping));
    }

    @Override
    public Guard getUniverse() {
        return name.getUniverse();
    }

    @Override
    public String toString() {
        String str = "{";
        int i = 0;
        for (GuardedValue<EventName> name : getName().getGuardedValues()) {
            //ScheduleLogger.log("name: " + name.value + " mach: " + this.guard(name.guard).getMachine());
            //if (getMachine().guard(name.guard).getGuardedValues().size() > 1) assert(false);
            str += name.value;
            //str += " -> " + getMachine().guard(name.guard);
            if (map.size() > 0 && map.containsKey(name.value)) {
                str += ": " + map.get(name.value);
            }
            if (i < getName().getGuardedValues().size() - 1)
                str += System.lineSeparator();
        }
        str += "}";
        return str;
    }

}
