package psymbolic.runtime.machine.buffer;

import psymbolic.runtime.Event;
import psymbolic.runtime.scheduler.Scheduler;
import psymbolic.runtime.logger.TraceLogger;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.Message;
import psymbolic.valuesummary.*;

import java.io.Serializable;
import java.util.function.Function;

/**
 * Represents an Event-Bag that is used to store the outgoing events at each state machine.
 */
public class EventBag extends SymbolicBag<Message> implements EventBuffer, Serializable {

    private final Machine sender;

    public EventBag(Machine sender) {
        super();
        this.sender = sender;
    }

    @Override
    public void send(Guard pc, PrimitiveVS<Machine> dest, PrimitiveVS<Event> eventName, UnionVS payload) {
        if (eventName.getGuardedValues().size() > 1) {
            throw new RuntimeException("Not Implemented");
        }
        TraceLogger.send(new Message(eventName, dest, payload).restrict(pc));
        if (sender != null)
            sender.incrementClock(pc);
        add(new Message(eventName, dest, payload, sender.getClock()).restrict(pc));
    }

    @Override
    public PrimitiveVS<Machine> create(Guard pc, Scheduler scheduler, Class<? extends Machine> machineType, UnionVS payload, Function<Integer, ? extends Machine> constructor) {
        PrimitiveVS<Machine> machine = scheduler.allocateMachine(pc, machineType, constructor);
        if (payload != null) payload = payload.restrict(pc);
        if (sender != null)
            sender.incrementClock(pc);
        add(new Message(Event.createMachine, machine, payload, sender.getClock()).restrict(pc));
        return machine;
    }

    @Override
    public PrimitiveVS<Boolean> satisfiesPredUnderGuard(Function<Message, PrimitiveVS<Boolean>> pred) {
        Guard cond = this.getElements().getNonEmptyUniverse();
        ListVS<Message> elts = getElements().restrict(cond);
        PrimitiveVS<Integer> idx = new PrimitiveVS<>(0).restrict(cond);
        while (BooleanVS.isEverTrue(IntegerVS.lessThan(idx, elts.size()))) {
            Guard iterCond = IntegerVS.lessThan(idx, elts.size()).getGuardFor(true);
            PrimitiveVS<Boolean> res = pred.apply(elts.get(idx.restrict(iterCond)));
            if (!res.getGuardFor(true).isFalse()) {
                return res;
            }
            idx = IntegerVS.add(idx, 1);
        }
        return new PrimitiveVS<>(false);
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

    @Override
    public void setEvents(ValueSummary events) { this.elements = (ListVS<Message>) events; }
}
