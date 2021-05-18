package psymbolic.runtime;

import psymbolic.runtime.logger.ScheduleLogger;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.Message;
import psymbolic.valuesummary.IntegerVS;
import psymbolic.valuesummary.PrimitiveVS;
import psymbolic.valuesummary.Guard;

import java.util.function.Function;

public class ReplayScheduler extends Scheduler {

    /** Schedule to replay */
    private final Schedule schedule;

    public ReplayScheduler (String name, Schedule schedule) {
        this(name, schedule, Guard.constTrue());
    }

    public ReplayScheduler (String name, Schedule schedule, Guard pc) {
        super(name);
        ScheduleLogger.enable();
        this.schedule = schedule.guard(pc).getSingleSchedule();
        for (Machine machine : schedule.getMachines()) {
            machine.reset();
        }
    }

    @Override
    public boolean isDone() {
        return super.isDone() || this.getDepth() >= schedule.size();
    }

    @Override
    public void startWith(Machine machine) {
        PrimitiveVS<Machine> machineVS;
        if (this.machineCounters.containsKey(machine.getClass())) {
            machineVS = schedule.getMachine(machine.getClass(), this.machineCounters.get(machine.getClass()));
            this.machineCounters.put(machine.getClass(),
                    IntegerVS.add(this.machineCounters.get(machine.getClass()), 1));
        } else {
            machineVS = schedule.getMachine(machine.getClass(), new PrimitiveVS<>(0));
            this.machineCounters.put(machine.getClass(), new PrimitiveVS<>(1));
        }

        ScheduleLogger.onCreateMachine(machineVS.getUniverse(), machine);
        machine.setScheduler(this);

        performEffect(
                new Message(
                        Event.Init,
                        machineVS,
                        null
                )
        );
    }

    @Override
    public PrimitiveVS<Machine> getNextSender() {
        PrimitiveVS<Machine> res = schedule.getRepeatSender(choiceDepth);
        choiceDepth++;
        return res;
    }

    @Override
    public PrimitiveVS<Boolean> getNextBoolean(Guard pc) {
        PrimitiveVS<Boolean> res = schedule.getRepeatBool(choiceDepth);
        choiceDepth++;
        return res;
    }

    @Override
    public PrimitiveVS<Integer> getNextInteger(PrimitiveVS<Integer> bound, Guard pc) {
        PrimitiveVS<Integer> res = schedule.getRepeatInt(choiceDepth);
        assert(IntegerVS.lessThan(res, bound).getGuardFor(false).isFalse());
        choiceDepth++;
        return res;
    }

    @Override
    public PrimitiveVS<Machine> allocateMachine(Guard pc, Class<? extends Machine> machineType,
                                           Function<Integer, ? extends Machine> constructor) {
        if (!machineCounters.containsKey(machineType)) {
            machineCounters.put(machineType, new PrimitiveVS<>(0));
        }
        PrimitiveVS<Integer> guardedCount = machineCounters.get(machineType).restrict(pc);

        PrimitiveVS<Machine> allocated = schedule.getMachine(machineType, guardedCount);
        ScheduleLogger.onCreateMachine(pc, allocated.getValues().iterator().next());
        allocated.getValues().iterator().next().setScheduler(this);

        guardedCount = IntegerVS.add(guardedCount, 1);

        PrimitiveVS<Integer> mergedCount = machineCounters.get(machineType).updateUnderGuard(pc, guardedCount);
        machineCounters.put(machineType, mergedCount);
        return allocated;
    }
}
