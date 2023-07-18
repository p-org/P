package psym.runtime.machine.buffer;

import java.util.function.Function;
import psym.runtime.machine.Machine;
import psym.runtime.machine.events.Event;
import psym.runtime.machine.events.Message;
import psym.runtime.scheduler.Scheduler;
import psym.valuesummary.Guard;
import psym.valuesummary.PrimitiveVS;
import psym.valuesummary.UnionVS;
import psym.valuesummary.ValueSummary;

/**
 * Represents an interface implemented by P state machine event buffer
 */
public interface EventBuffer {
    void send(Guard pc, PrimitiveVS<Machine> dest, PrimitiveVS<Event> eventName, UnionVS payload);

    PrimitiveVS<Machine> create(
            Guard pc,
            Scheduler scheduler,
            Class<? extends Machine> machineType,
            UnionVS payload,
            Function<Integer, ? extends Machine> constructor
    );

    void unblock(Message event);

    PrimitiveVS<Integer> size();

    boolean isEmpty();

    void add(Message e);

    PrimitiveVS<Boolean> satisfiesPredUnderGuard(Function<Message, PrimitiveVS<Boolean>> pred);

    PrimitiveVS<Boolean> hasCreateMachineUnderGuard();

    Message remove(Guard pc);

    Message peek(Guard pc);

    default PrimitiveVS<Machine> create(Guard pc, Scheduler scheduler, Class<? extends Machine> machineType,
                                        Function<Integer, ? extends Machine> constructor) {
        return create(pc, scheduler, machineType, null, constructor);
    }

    PrimitiveVS<Boolean> hasSyncEventUnderGuard();

    ValueSummary getEvents();

    void setEvents(ValueSummary events);
}
