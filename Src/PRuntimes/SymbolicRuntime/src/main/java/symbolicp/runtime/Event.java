package symbolicp.runtime;


import symbolicp.bdd.Bdd;
import symbolicp.vs.*;

import java.util.*;


public class Event implements ValueSummary<Event> {

    private final PrimVS<EventName> name;
    private final Map<EventName, UnionVS> map;
    private final PrimVS<Machine> machine;
    private final VectorClockVS clock;

    public PrimVS<Boolean> canRun() {
        Bdd cond = Bdd.constFalse();
        for (GuardedValue<Machine> machine : getMachine().getGuardedValues()) {
            cond = cond.or(machine.value.hasStarted().getGuard(true).and(machine.guard));

            if (BoolUtils.isEverFalse(machine.value.hasStarted())) {
                Bdd unstarted = machine.value.hasStarted().getGuard(false).and(machine.guard);
                PrimVS<EventName> names = this.guard(unstarted).getName();
                for (GuardedValue<EventName> name : names.getGuardedValues()) {
                    if (name.value.equals(EventName.Init.instance)) {
                        cond = cond.or(name.guard);
                    }
                }
            }
        }
        return BoolUtils.fromTrueGuard(cond);
    }

    public PrimVS<Boolean> isInit() {
        Bdd cond = Bdd.constFalse();
        for (GuardedValue<Machine> machine : getMachine().getGuardedValues()) {
            if (BoolUtils.isEverFalse(machine.value.hasStarted())) {
                Bdd unstarted = machine.value.hasStarted().getGuard(false).and(machine.guard);
                PrimVS<EventName> names = this.guard(unstarted).getName();
                for (GuardedValue<EventName> name : names.getGuardedValues()) {
                    if (name.value.equals(EventName.Init.instance)) {
                        cond = cond.or(name.guard);
                    }
                }
            }
        }
        return BoolUtils.fromTrueGuard(cond);
    }

    public Event getForMachine(Machine machine) {
        Bdd cond = this.machine.getGuard(machine);
        return this.guard(cond);
    }

    private Event(PrimVS<EventName> names, VectorClockVS clock, PrimVS<Machine> machine, Map<EventName, UnionVS> map) {
        this.name = names;
        this.machine = machine;
        this.map = new HashMap<>(map);
        this.clock = clock;
    }

    public Event(EventName name, VectorClockVS clock, PrimVS<Machine> machine) {
        this(new PrimVS<>(name), clock, machine, new HashMap<>());
    }

    public Event(PrimVS<EventName> name, VectorClockVS clock, PrimVS<Machine> machine) {
        this(name, clock, machine, new HashMap<>());
    }

    public Event() {
        this(new PrimVS<>(), new VectorClockVS(Bdd.constFalse()), new PrimVS<>());
    }

    public Event(EventName name, VectorClockVS clock, PrimVS<Machine> machine, UnionVS payload) {
        this(new PrimVS<>(name),clock, machine, payload);
    }

    public Event(PrimVS<EventName> names, VectorClockVS clock, PrimVS<Machine> machine, UnionVS payload) {
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

    public PrimVS<EventName> getName() {
        return this.name;
    }

    public PrimVS<Machine> getMachine() {
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
    public Event guard(Bdd guard) {
        Map<EventName, UnionVS> newMap = new HashMap<>();
        PrimVS<EventName> newName = name.guard(guard);
        for (Map.Entry<EventName, UnionVS> entry : map.entrySet()) {
            if (!newName.getGuard(entry.getKey()).isConstFalse()) {
                newMap.put(entry.getKey(), entry.getValue().guard(guard));
            }
        }
        return new Event(name.guard(guard), clock.guard(guard), machine.guard(guard), newMap);
    }

    @Override
    public Event merge(Iterable<Event> summaries) {
        List<PrimVS<EventName>> namesToMerge = new ArrayList<>();
        List<VectorClockVS> clocksToMerge = new ArrayList<>();
        List<PrimVS<Machine>> machinesToMerge = new ArrayList<>();
        Map<EventName, UnionVS> newMap = new HashMap<>();

        for (Map.Entry<EventName, UnionVS> entry : this.map.entrySet()) {
            if (!name.getGuard(entry.getKey()).isConstFalse() && entry.getValue() != null) {
                newMap.put(entry.getKey(), entry.getValue());
            }
        }

        for (Event summary : summaries) {
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

        return new Event(name.merge(namesToMerge), clock.merge(clocksToMerge), machine.merge(machinesToMerge), newMap);
    }

    @Override
    public Event merge(Event summary) {
        assert (this.getUniverse().and(summary.getUniverse()).isConstFalse());
        return merge(Collections.singletonList(summary));
    }

    @Override
    public Event update(Bdd guard, Event update) {
        /*
        if (guard.isConstTrue()) {
            assert (this.guard(guard.not()).merge(update.guard(guard)).symbolicEquals(update, guard).getGuard(true).isConstTrue());
        }*/
        return this.guard(guard.not()).merge(update.guard(guard));
    }

    @Override
    public PrimVS<Boolean> symbolicEquals(Event cmp, Bdd pc) {
        Bdd nameAndTarget = this.name.symbolicEquals(cmp.name, Bdd.constTrue()).getGuard(true).and(
                                this.machine.symbolicEquals(cmp.machine, Bdd.constTrue()).getGuard(true));
        Bdd mapping = Bdd.constFalse();
        for (GuardedValue<EventName> event : name.getGuardedValues()) {
            if (this.map.containsKey(event.value)) {
                if (cmp.map.containsKey(event.value)) {
                    mapping = mapping.or(this.map.get(event.value)
                                                  .symbolicEquals(cmp.map.get(event.value), Bdd.constTrue())
                                                  .getGuard(true));
                }
            } else if (!cmp.map.containsKey(event.value)) {
                mapping = mapping.or(event.guard);
            }
        }
        return BoolUtils.fromTrueGuard(pc.and(nameAndTarget).and(mapping));
    }

    @Override
    public Bdd getUniverse() {
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
        str += ": CLOCK " + this.clock.toString();
        return str;
    }

}
