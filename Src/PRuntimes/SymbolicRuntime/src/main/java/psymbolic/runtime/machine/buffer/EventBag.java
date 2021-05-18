package psymbolic.runtime.machine.buffer;

import psymbolic.runtime.Event;
import psymbolic.runtime.Scheduler;
import psymbolic.runtime.logger.ScheduleLogger;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.Message;
import psymbolic.valuesummary.*;

import java.util.function.Function;

/**
 * Represents an Event-Bag that is used to store the outgoing events at each state machine.
 */
public class EventBag extends SymbolicBag<Message> implements EventBuffer {

    public EventBag(Machine sender) {
        super();
    }

    @Override
    public void send(Guard pc, PrimitiveVS<Machine> dest, PrimitiveVS<Event> event, UnionVS payload) {
        ScheduleLogger.send(new Message(event, dest, payload).restrict(pc));
        this.add(new Message(event, dest, payload).restrict(pc));
    }

    @Override
    public PrimitiveVS<Machine> create(Guard pc, Scheduler scheduler, Class<? extends Machine> machineType, UnionVS payload, Function<Integer, ? extends Machine> constructor) {
        PrimitiveVS<Machine> machine = scheduler.allocateMachine(pc, machineType, constructor);
        if (payload != null) payload = payload.restrict(pc);
        add(new Message(Event.Init, machine, payload).restrict(pc));
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
    public PrimitiveVS<Boolean> isInitUnderGuard() {
        return satisfiesPredUnderGuard(Message::isInit);
    }
}
