package psymbolic.runtime.machine.buffer;

import psymbolic.runtime.Event;
import psymbolic.runtime.EventName;
import psymbolic.runtime.Scheduler;
import psymbolic.runtime.machine.Message;
import psymbolic.valuesummary.UnionVS;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.PrimitiveVS;
import psymbolic.runtime.machine.Machine;

import java.util.function.Function;

/**
 * Represents an Event-Bag that is used to store the outgoing events at each state machine.
 */
public class EventBag extends SymbolicBag<Event> implements EventBuffer {

    // the source or sender state machine that is sending the events
    private final Machine sender;

    public EventBag(Machine sender) {
        super();
        this.sender = sender;
    }

    @Override
    public void send(Guard pc, PrimitiveVS<Machine> dest, PrimitiveVS<Event> event, UnionVS payload) {
        ScheduleLogger.send(new Message(event, dest, payload).restrict(pc));
        this.add(new Message(event, dest, payload).restrict(pc));
    }

    @Override
    public PrimitiveVS<Machine> create(Guard pc, Scheduler scheduler, Class<? extends Machine> machineType, UnionVS payload, Function<Integer, ? extends Machine> constructor) {
        PrimitiveVS<Machine> machine = scheduler.allocateMachine(pc, machineType, constructor);
        if (payload != null) payload = payload.guard(pc);
        add(new Event(EventName.Init.instance, src.getClock(), machine, payload).guard(pc));
        return machine;
    }

    @Override
    public PrimitiveVS<Boolean> enabledCondInit() {
        return satisfiesPredUnderGuard(Event::isInit);
    }

}
