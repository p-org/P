package psymbolic.runtime.machine.buffer;

import psymbolic.runtime.Event;
import psymbolic.runtime.scheduler.Scheduler;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.Message;
import psymbolic.valuesummary.Guard;
import psymbolic.valuesummary.PrimitiveVS;
import psymbolic.valuesummary.UnionVS;
import psymbolic.valuesummary.ValueSummary;

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
}
