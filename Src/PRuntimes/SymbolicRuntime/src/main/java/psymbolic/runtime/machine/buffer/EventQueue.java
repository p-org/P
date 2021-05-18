package psymbolic.runtime.machine.buffer;

import psymbolic.runtime.Event;
import psymbolic.runtime.Scheduler;
import psymbolic.runtime.logger.ScheduleLogger;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.Message;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.PrimitiveVS;
import psymbolic.valuesummary.UnionVS;

import java.util.function.Function;

public class EventQueue extends SymbolicQueue<Message> implements EventBuffer {

    public EventQueue(Machine sender) {
        super();
    }

    public void send(Guard pc, PrimitiveVS<Machine> dest, PrimitiveVS<Event> eventName, UnionVS payload) {
        if (eventName.getGuardedValues().size() > 1) {
            throw new RuntimeException("Not Implemented");
        }
        ScheduleLogger.send(new Message(eventName, dest, payload).restrict(pc));
        enqueue(new Message(eventName, dest, payload).restrict(pc));
    }

    public PrimitiveVS<Machine> create(
            Guard pc,
            Scheduler scheduler,
            Class<? extends Machine> machineType,
            UnionVS payload,
            Function<Integer, ? extends Machine> constructor
    ) {
        PrimitiveVS<Machine> machine = scheduler.allocateMachine(pc, machineType, constructor);
        if (payload != null) payload = payload.restrict(pc);
        enqueue(new Message(Event.Init, machine, payload).restrict(pc));
        return machine;
    }

    @Override
    public void add(Message e) {
        enqueue(e);
    }

    @Override
    public PrimitiveVS<Boolean> satisfiesPredUnderGuard(Function<Message, PrimitiveVS<Boolean>> pred) {
        Guard cond = isEnabledUnderGuard();
        assert(!cond.isFalse());
        Message top = peek(cond);
        return pred.apply(top).restrict(top.getUniverse());
    }

    @Override
    public Message remove(Guard pc) {
        return dequeueEntry(pc);
    }

    @Override
    public PrimitiveVS<Machine> create(Guard pc, Scheduler scheduler, Class<? extends Machine> machineType, Function<Integer, ? extends Machine> constructor) {
        return EventBuffer.super.create(pc, scheduler, machineType, constructor);
    }

    @Override
    public PrimitiveVS<Boolean> isInitUnderGuard() {
        return satisfiesPredUnderGuard(Message::isInit);
    }
}
