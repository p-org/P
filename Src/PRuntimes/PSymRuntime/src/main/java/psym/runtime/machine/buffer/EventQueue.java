package psym.runtime.machine.buffer;

import psym.runtime.Event;
import psym.runtime.Message;
import psym.runtime.logger.TraceLogger;
import psym.runtime.machine.Machine;
import psym.runtime.scheduler.Scheduler;
import psym.valuesummary.*;

import java.io.Serializable;
import java.util.function.Function;

public class EventQueue extends SymbolicQueue<Message> implements EventBuffer, Serializable {

    private final Machine sender;

    public EventQueue(Machine sender) {
        super();
        this.sender = sender;
    }

    public void send(Guard pc, PrimitiveVS<Machine> dest, PrimitiveVS<Event> eventName, UnionVS payload) {
        if (eventName.getGuardedValues().size() > 1) {
            throw new RuntimeException(String.format("Handling multiple events together is not supported, in %s", eventName));
        }
        TraceLogger.send(new Message(eventName, dest, payload).restrict(pc));
        Message event = new Message(eventName, dest, payload).restrict(pc);
        enqueue(event);
        sender.getScheduler().runMonitors(event);
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
        Message event = new Message(Event.createMachine, machine, payload).restrict(pc);
        enqueue(event);
//        scheduler.performEffect(event);
        return machine;
    }

    @Override
    public void add(Message e) {
        enqueue(e);
    }

    @Override
    public PrimitiveVS<Boolean> satisfiesPredUnderGuard(Function<Message, PrimitiveVS<Boolean>> pred) {
        Guard cond = isEnabledUnderGuard();
        assert (!cond.isFalse());
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
    public ValueSummary getEvents() {
        return this.elements;
    }

    @Override
    public void setEvents(ValueSummary events) {
        this.elements = (ListVS<Message>) events;
        resetPeek();
    }
}
