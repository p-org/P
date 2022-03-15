package psymbolic.runtime.machine.buffer;

import psymbolic.runtime.Event;
import psymbolic.runtime.scheduler.Scheduler;
import psymbolic.runtime.logger.TraceLogger;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.Message;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.PrimitiveVS;
import psymbolic.valuesummary.UnionVS;
import psymbolic.valuesummary.ValueSummary;

import java.util.function.Function;

public class EventQueue extends SymbolicQueue<Message> implements EventBuffer {

    private final Machine sender;

    public EventQueue(Machine sender) {
        super();
        this.sender = sender;
    }

    public void send(Guard pc, PrimitiveVS<Machine> dest, PrimitiveVS<Event> eventName, UnionVS payload) {
        if (eventName.getGuardedValues().size() > 1) {
            throw new RuntimeException("Not Implemented");
        }
        TraceLogger.send(new Message(eventName, dest, payload).restrict(pc));
        if (sender != null)
            sender.incrementClock(pc);
        if (sender.getScheduler().useSleepSets()) {
            sender.getScheduler().getSchedule().unblock(sender.getClock());
        }
        enqueue(new Message(eventName, dest, payload, sender.getClock()).restrict(pc));
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
        if (sender != null)
            sender.incrementClock(pc);
        enqueue(new Message(Event.createMachine, machine, payload, sender.getClock()).restrict(pc));
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
    public PrimitiveVS<Boolean> hasCreateMachineUnderGuard() {
        return satisfiesPredUnderGuard(Message::isCreateMachine);
    }

    @Override
    public PrimitiveVS<Boolean> hasSyncEventUnderGuard() {
        return satisfiesPredUnderGuard(Message::isSyncEvent);
    }

    @Override
    public ValueSummary getEvents() { return this.elements; }
}
