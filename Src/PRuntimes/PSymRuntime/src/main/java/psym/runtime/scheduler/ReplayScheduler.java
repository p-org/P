package psym.runtime.scheduler;

import lombok.Getter;
import psym.commandline.PSymConfiguration;
import psym.commandline.Program;
import psym.runtime.Event;
import psym.runtime.Message;
import psym.runtime.logger.PSymLogger;
import psym.runtime.logger.TraceLogger;
import psym.runtime.machine.Machine;
import psym.utils.GlobalData;
import psym.valuesummary.*;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.concurrent.TimeoutException;
import java.util.function.Function;

public class ReplayScheduler extends Scheduler {
    @Getter
    /** Path constraint */
    private final Guard pathConstraint;
    @Getter
    /** Flag for liveness bug */
    private final boolean isLivenessBug;
    /**
     * Counterexample length
     */
    private int cexLength = 0;

    public ReplayScheduler(PSymConfiguration config, Program p, Schedule schedule, int length, boolean livenessBug) {
        this(config, p, schedule, Guard.constTrue(), length, livenessBug);
    }

    public ReplayScheduler(PSymConfiguration config, Program p, Schedule schedule, Guard pc, int length, boolean livenessBug) {
        super(config, p);
        TraceLogger.enable();
        this.schedule = schedule.guard(pc).getSingleSchedule();
        for (Machine machine : schedule.getMachines()) {
            machine.reset();
        }
        configuration.setCollectStats(0);
        cexLength = length;
        pathConstraint = pc;
        isLivenessBug = livenessBug;
    }

    /**
     * Read scheduler state from a file
     *
     * @param readFromFile Name of the input file containing the scheduler state
     * @return A scheduler object
     * @throws Exception Throw error if reading fails
     */
    public static ReplayScheduler readFromFile(String readFromFile) throws Exception {
        ReplayScheduler result;
        try {
            PSymLogger.info(".. Reading replayer state from file " + readFromFile);
            FileInputStream fis;
            fis = new FileInputStream(readFromFile);
            ObjectInputStream ois = new ObjectInputStream(fis);
            result = (ReplayScheduler) ois.readObject();
            GlobalData.setInstance((GlobalData) ois.readObject());
            result.reinitialize();
            PSymLogger.info(".. Successfully read.");
        } catch (IOException | ClassNotFoundException e) {
            e.printStackTrace();
            throw new Exception(".. Failed to read replayer state from file " + readFromFile);
        }
        return result;
    }

    /**
     * Write scheduler state to a file
     *
     * @param writeFileName Output file name
     * @throws Exception Throw error if writing fails
     */
    public void writeToFile(String writeFileName) throws RuntimeException {
        try {
            FileOutputStream fos = new FileOutputStream(writeFileName);
            ObjectOutputStream oos = new ObjectOutputStream(fos);
            oos.writeObject(this);
            oos.writeObject(GlobalData.getInstance());
            if (configuration.getVerbosity() > 0) {
                long szBytes = Files.size(Paths.get(writeFileName));
                PSymLogger.info(String.format("  %,.1f MB  written in %s", (szBytes / 1024.0 / 1024.0), writeFileName));
            }
        } catch (IOException e) {
            e.printStackTrace();
            throw new RuntimeException("Failed to write replayer in file " + writeFileName);
        }
    }

    @Override
    public void reinitialize() {
        for (Machine machine : schedule.getMachines()) {
            machine.reset();
        }
        super.reinitialize();
    }

    @Override
    public void doSearch() throws TimeoutException, InterruptedException {
        TraceLogger.logStartReplayCex(cexLength);
        super.doSearch();
        checkLiveness(isLivenessBug);
    }

    @Override
    public boolean isDone() {
        return super.isDone() || this.getChoiceDepth() >= schedule.size();
    }

    @Override
    public boolean isFinishedExecution() {
        return super.isFinishedExecution() || this.getChoiceDepth() >= schedule.size();
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
