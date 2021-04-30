package psymbolic.runtime;

import psymbolic.valuesummary.bdd.Bdd;
import psymbolic.valuesummary.*;

import java.util.function.Function;

public class EffectBag extends SymbolicBag<Event> implements EffectCollection {

    private Machine src;

    public EffectBag(Machine src) {
        super();
        this.src = src;
    }

    @Override
    public void send(Bdd pc, PrimVS<Machine> dest, PrimVS<EventName> eventName, UnionVS payload) {
        ScheduleLogger.send(new Event(eventName, src.getClock(), dest, payload).guard(pc));
        this.add(new Event(eventName, src.getClock(), dest, payload).guard(pc));
        if (src != null)
            src.incrementClock(pc);
    }

    @Override
    public PrimVS<Machine> create(Bdd pc, Scheduler scheduler, Class<? extends Machine> machineType, UnionVS payload, Function<Integer, ? extends Machine> constructor) {
        PrimVS<Machine> machine = scheduler.allocateMachine(pc, machineType, constructor);
        if (payload != null) payload = payload.guard(pc);
        add(new Event(EventName.Init.instance, src.getClock(), machine, payload).guard(pc));
        return machine;
    }

    @Override
    public PrimVS<Boolean> enabledCondInit() {
        return enabledCondOne(Event::isInit);
    }

}
