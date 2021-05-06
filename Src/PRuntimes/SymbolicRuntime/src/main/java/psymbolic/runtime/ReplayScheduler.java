package psymbolic.runtime;

import p.runtime.values.PBool;
import p.runtime.values.PInt;
import psymbolic.valuesummary.IntUtils;
import psymbolic.valuesummary.PIntUtils;
import psymbolic.valuesummary.PrimVS;
import psymbolic.valuesummary.VectorClockVS;
import psymbolic.valuesummary.bdd.Bdd;

import java.util.function.Function;

public class ReplayScheduler extends Scheduler {

    /** Schedule to replay */
    private final Schedule schedule;

    public ReplayScheduler (String name, Schedule schedule) {
        this(name, schedule, Bdd.constTrue());
    }

    public ReplayScheduler (String name, Schedule schedule, Bdd pc) {
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
        PrimVS<Machine> machineVS;
        if (this.machineCounters.containsKey(machine.getClass())) {
            machineVS = schedule.getMachine(machine.getClass(), this.machineCounters.get(machine.getClass()));
            this.machineCounters.put(machine.getClass(),
                    IntUtils.add(this.machineCounters.get(machine.getClass()), 1));
        } else {
            machineVS = schedule.getMachine(machine.getClass(), new PrimVS<>(0));
            this.machineCounters.put(machine.getClass(), new PrimVS<>(1));
        }

        ScheduleLogger.onCreateMachine(machineVS.getUniverse(), machine);
        machine.setScheduler(this);

        performEffect(
                new Event(
                        EventName.Init.instance,
                        new VectorClockVS(Bdd.constTrue()),
                        machineVS,
                        null
                )
        );
    }

    @Override
    public PrimVS<Machine> getNextSender() {
        PrimVS<Machine> res = schedule.getRepeatSender(choiceDepth);
        choiceDepth++;
        return res;
    }

    @Override
    public PrimVS<PBool> getNextBoolean(Bdd pc) {
        PrimVS<PBool> res = schedule.getRepeatBool(choiceDepth);
        choiceDepth++;
        return res;
    }

    @Override
    public PrimVS<PInt> getNextInteger(PrimVS<PInt> bound, Bdd pc) {
        PrimVS<PInt> res = schedule.getRepeatInt(choiceDepth);
        assert(PIntUtils.lessThan(res, bound).getGuard(false).isConstFalse());
        choiceDepth++;
        return res;
    }

    @Override
    public PrimVS<Machine> allocateMachine(Bdd pc, Class<? extends Machine> machineType,
                                           Function<Integer, ? extends Machine> constructor) {
        if (!machineCounters.containsKey(machineType)) {
            machineCounters.put(machineType, new PrimVS<>(0));
        }
        PrimVS<Integer> guardedCount = machineCounters.get(machineType).guard(pc);

        PrimVS<Machine> allocated = schedule.getMachine(machineType, guardedCount);
        ScheduleLogger.onCreateMachine(pc, allocated.getValues().iterator().next());
        allocated.getValues().iterator().next().setScheduler(this);

        guardedCount = IntUtils.add(guardedCount, 1);

        PrimVS<Integer> mergedCount = machineCounters.get(machineType).update(pc, guardedCount);
        machineCounters.put(machineType, mergedCount);
        return allocated;
    }
}
