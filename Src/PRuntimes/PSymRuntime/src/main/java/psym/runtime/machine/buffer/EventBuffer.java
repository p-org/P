package psym.runtime.machine.buffer;

import psym.runtime.Event;
import psym.runtime.scheduler.Scheduler;
import psym.runtime.machine.Machine;
import psym.runtime.Message;
import psym.valuesummary.Guard;
import psym.valuesummary.PrimitiveVS;
import psym.valuesummary.UnionVS;
import psym.valuesummary.ValueSummary;

import java.util.function.Function;

/**
 * Represents an interface implemented by P state machine event buffer
 */
public interface EventBuffer {
    public void send(Guard pc, PrimitiveVS<Machine> dest, PrimitiveVS<Event> eventName, UnionVS payload);

    public PrimitiveVS<Machine> create(
            Guard pc,
            Scheduler scheduler,
            Class<? extends Machine> machineType,
            UnionVS payload,
            Function<Integer, ? extends Machine> constructor
    );

    public PrimitiveVS<Integer> size();

    boolean isEmpty();

    public void add(Message e);

    public PrimitiveVS<Boolean> satisfiesPredUnderGuard(Function<Message, PrimitiveVS<Boolean>> pred);

    public PrimitiveVS<Boolean> hasCreateMachineUnderGuard();

    public Message remove(Guard pc);

    public Message peek(Guard pc);

    default public PrimitiveVS<Machine> create(Guard pc, Scheduler scheduler, Class<? extends Machine> machineType,
                                  Function<Integer, ? extends Machine> constructor) {
        return create(pc, scheduler, machineType, null, constructor);
    }

    PrimitiveVS<Boolean> hasSyncEventUnderGuard();

    public ValueSummary getEvents();

    public void setEvents(ValueSummary events);
}
