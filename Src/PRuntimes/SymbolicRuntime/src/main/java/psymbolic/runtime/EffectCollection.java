package psymbolic.runtime;

import psymbolic.valuesummary.PrimVS;
import psymbolic.valuesummary.UnionVS;
import psymbolic.valuesummary.bdd.Bdd;

import java.util.function.Function;

public interface EffectCollection {
    public void send(Bdd pc, PrimVS<Machine> dest, PrimVS<EventName> eventName, UnionVS payload);

    public PrimVS<Machine> create(
            Bdd pc,
            Scheduler scheduler,
            Class<? extends Machine> machineType,
            UnionVS payload,
            Function<Integer, ? extends Machine> constructor
    );

    public PrimVS<Integer> size();

    public boolean isEmpty();

    public void add(Event e);

    public Event remove(Bdd pc);

    public Event peek(Bdd pc);

    public PrimVS<Boolean> enabledCond(Function<Event, PrimVS<Boolean>> pred);

    public PrimVS<Boolean> enabledCondInit();

    default public PrimVS<Machine> create(Bdd pc, Scheduler scheduler, Class<? extends Machine> machineType,
                                  Function<Integer, ? extends Machine> constructor) {
        return create(pc, scheduler, machineType, null, constructor);
    }
}
