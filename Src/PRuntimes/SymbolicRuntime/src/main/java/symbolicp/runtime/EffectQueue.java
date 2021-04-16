package symbolicp.runtime;

import symbolicp.bdd.Bdd;
import symbolicp.util.Checks;
import symbolicp.util.NotImplementedException;
import symbolicp.vs.*;

import java.util.function.Function;

public class EffectQueue extends SymbolicQueue<Event> implements EffectCollection {

    private Machine src;

    public EffectQueue(Machine src) {
        super();
        this.src = src;
    }

    public void send(Bdd pc, PrimVS<Machine> dest, PrimVS<EventName> eventName, UnionVS payload) {
        if (eventName.getGuardedValues().size() > 1) {
            throw new NotImplementedException();
        }
        ScheduleLogger.send(new Event(eventName, src.getClock(), dest, payload).guard(pc));
        enqueueEntry(new Event(eventName, src.getClock(), dest, payload).guard(pc));
        if (src != null)
            src.incrementClock(pc);
    }

    public PrimVS<Machine> create(
            Bdd pc,
            Scheduler scheduler,
            Class<? extends Machine> machineType,
            UnionVS payload,
            Function<Integer, ? extends Machine> constructor
    ) {
        PrimVS<Machine> machine = scheduler.allocateMachine(pc, machineType, constructor);
        if (payload != null) payload = payload.guard(pc);
        enqueueEntry(new Event(EventName.Init.instance, src.getClock(), machine, payload).guard(pc));
        return machine;
    }

    @Override
    public void add(Event e) {
        this.enqueueEntry(e);
    }

    @Override
    public Event remove(Bdd pc) {
        return this.dequeueEntry(pc);
    }

    @Override
    public PrimVS<Boolean> enabledCondInit() {
        return enabledCond(Event::isInit);
    }

}
