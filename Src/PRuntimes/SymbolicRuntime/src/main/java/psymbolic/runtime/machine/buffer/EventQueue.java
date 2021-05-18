package psymbolic.runtime.machine.buffer;

import psymbolic.runtime.Event;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.Scheduler;
import psymbolic.runtime.SymbolicQueue;
import psymbolic.valuesummary.PrimitiveVS
import psymbolic.valuesummary.UnionVS;
import psymbolic.valuesummary.bdd.Bdd;

import java.util.function.Function;

public class EventQueue extends SymbolicQueue<Event> implements EventBuffer {

    private Machine src;

    public EventQueue(Machine src) {
        super();
        this.src = src;
    }

    public void send(Bdd pc, PrimVS<Machine> dest, PrimVS<EventName> eventName, UnionVS payload) {
        if (eventName.getGuardedValues().size() > 1) {
            throw new RuntimeException("Not Implemented");
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
