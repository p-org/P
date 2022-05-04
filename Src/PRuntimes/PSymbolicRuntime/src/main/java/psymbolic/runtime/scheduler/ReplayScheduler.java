package psymbolic.runtime.scheduler;

import psymbolic.commandline.PSymConfiguration;
import psymbolic.commandline.Program;
import psymbolic.runtime.Event;
import psymbolic.runtime.logger.TraceLogger;
import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.Message;
import psymbolic.valuesummary.*;

import java.util.function.Function;

public class ReplayScheduler extends Scheduler {

    /** Schedule to replay */
    private final Schedule schedule;

    /** Counterexample length */
    private int cexLength = 0;

    public ReplayScheduler (PSymConfiguration config, Schedule schedule, int length) {
        this(config, schedule, Guard.constTrue(), length);
    }

    public ReplayScheduler (PSymConfiguration config, Schedule schedule, Guard pc, int length) {
        super(config);
        TraceLogger.enable();
        this.schedule = schedule.guard(pc).getSingleSchedule();
        for (Machine machine : schedule.getMachines()) {
            machine.reset();
        }
        configuration.setUseReceiverQueueSemantics(false);
        configuration.setCollectStats(0);
        getVcManager().disable();
        cexLength = length;
    }

    @Override
    public void doSearch(Program p) {
        TraceLogger.logStartReplayCex(cexLength);
        super.doSearch(p);
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

        TraceLogger.onCreateMachine(machineVS.getUniverse(), machine);
        machine.setScheduler(this);
        performEffect(
                new Message(
                        Event.createMachine,
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
    public ValueSummary getNextElement(ListVS<? extends ValueSummary> candidates, Guard pc) {
        ValueSummary res = getNextElementFlattener(schedule.getRepeatElement(choiceDepth));
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
        TraceLogger.onCreateMachine(pc, allocated.getValues().iterator().next());
        allocated.getValues().iterator().next().setScheduler(this);

        guardedCount = IntegerVS.add(guardedCount, 1);

        PrimitiveVS<Integer> mergedCount = machineCounters.get(machineType).updateUnderGuard(pc, guardedCount);
        machineCounters.put(machineType, mergedCount);
        return allocated;
    }
}
