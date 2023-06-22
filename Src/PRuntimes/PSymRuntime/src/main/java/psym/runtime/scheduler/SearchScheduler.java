package psym.runtime.scheduler;

import psym.commandline.PSymConfiguration;
import psym.runtime.Program;
import psym.runtime.logger.*;
import psym.runtime.machine.Machine;
import psym.runtime.scheduler.symmetry.SymmetryMode;
import psym.runtime.statistics.SearchStats;
import psym.runtime.GlobalData;
import psym.utils.monitor.MemoryMonitor;
import psym.utils.random.NondetUtil;
import psym.valuesummary.*;

import java.time.Instant;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.function.BiConsumer;
import java.util.function.Consumer;
import java.util.function.Function;
import java.util.function.Supplier;

/**
 * Represents the search scheduler
 */
public abstract class SearchScheduler extends Scheduler {
    protected transient Instant lastReportTime = Instant.now();

    protected SearchScheduler(PSymConfiguration config, Program p) {
        super(config, p);
    }
    protected abstract PrimitiveVS getNext(int depth, Function<Integer, PrimitiveVS> getRepeat, Function<Integer, List> getBacktrack,
                                           Consumer<Integer> clearBacktrack, BiConsumer<PrimitiveVS, Integer> addRepeat,
                                           BiConsumer<List, Integer> addBacktrack, Supplier<List> getChoices,
                                           Function<List, PrimitiveVS> generateNext, boolean isData);
    protected abstract void summarizeIteration(int startDepth) throws InterruptedException;
    protected abstract void recordResult(SearchStats.TotalStats totalStats);
    protected abstract void printCurrentStatus(double newRuntime);
    protected abstract void printProgressHeader(boolean consolePrint);
    protected abstract void printProgress(boolean forcePrint);
    public abstract void print_search_stats();

    @Override
    public PrimitiveVS<Machine> getNextSender() {
        int depth = choiceDepth;
        PrimitiveVS<Machine> res = getNext(depth, schedule::getRepeatSender, schedule::getBacktrackSender,
                schedule::clearBacktrack, schedule::addRepeatSender, schedule::addBacktrackSender, this::getNextSenderChoices,
                this::getNextSenderSummary, false);

        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public PrimitiveVS<Boolean> getNextBoolean(Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<Boolean> res = getNext(depth, schedule::getRepeatBool, schedule::getBacktrackBool,
                schedule::clearBacktrack, schedule::addRepeatBool, schedule::addBacktrackBool,
                () -> super.getNextBooleanChoices(pc), super::getNextBoolean, true);
        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public PrimitiveVS<Integer> getNextInteger(PrimitiveVS<Integer> bound, Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<Integer> res = getNext(depth, schedule::getRepeatInt, schedule::getBacktrackInt,
                schedule::clearBacktrack, schedule::addRepeatInt, schedule::addBacktrackInt,
                () -> super.getNextIntegerChoices(bound, pc), super::getNextInteger, true);
        choiceDepth = depth + 1;
        return res;
    }

    @Override
    public ValueSummary getNextElement(ListVS<? extends ValueSummary> candidates, Guard pc) {
        int depth = choiceDepth;
        PrimitiveVS<ValueSummary> res = getNext(depth, schedule::getRepeatElement, schedule::getBacktrackElement,
                schedule::clearBacktrack, schedule::addRepeatElement, schedule::addBacktrackElement,
                () -> super.getNextElementChoices(candidates, pc), super::getNextElementHelper, true);
        choiceDepth = depth + 1;
        return super.getNextElementFlattener(res);
    }

    @Override
    public PrimitiveVS<Machine> allocateMachine(Guard pc, Class<? extends Machine> machineType,
                                                Function<Integer, ? extends Machine> constructor) {
        if (!machineCounters.containsKey(machineType)) {
            machineCounters.put(machineType, new PrimitiveVS<>(0));
        }
        PrimitiveVS<Integer> guardedCount = machineCounters.get(machineType).restrict(pc);

        PrimitiveVS<Machine> allocated;
        if (schedule.hasMachine(machineType, guardedCount, pc)) {
            allocated = schedule.getMachine(machineType, guardedCount).restrict(pc);
            for (GuardedValue gv : allocated.getGuardedValues()) {
                Guard g = gv.getGuard();
                Machine m = (Machine) gv.getValue();
                assert (!BooleanVS.isEverTrue(m.hasStarted().restrict(g)));
                TraceLogger.onCreateMachine(pc.and(g), m);
                if (!machines.contains(m)) {
                    machines.add(m);
                }
                currentMachines.add(m);
                assert (machines.size() >= currentMachines.size());
                m.setScheduler(this);
                if (configuration.getSymmetryMode() != SymmetryMode.None) {
                    GlobalData.getSymmetryTracker().createMachine(m, g);
                }
            }
        } else {
            Machine newMachine = setupNewMachine(pc, guardedCount, constructor);

            allocated = new PrimitiveVS<>(newMachine).restrict(pc);
            if (configuration.getSymmetryMode() != SymmetryMode.None) {
                GlobalData.getSymmetryTracker().createMachine(newMachine, pc);
            }
        }

        guardedCount = IntegerVS.add(guardedCount, 1);

        PrimitiveVS<Integer> mergedCount = machineCounters.get(machineType).updateUnderGuard(pc, guardedCount);
        machineCounters.put(machineType, mergedCount);
        return allocated;
    }

    public void print_stats(SearchStats.TotalStats totalStats, double timeUsed, double memoryUsed) {
        printProgress(true);
        if (!isFinalResult) {
            recordResult(totalStats);
            isFinalResult = true;
        }

        SearchLogger.log("\n--------------------");

        // print basic statistics
        StatWriter.log("result", String.format("%s", result));
        StatWriter.log("time-seconds", String.format("%.1f", timeUsed));
        StatWriter.log("memory-max-MB", String.format("%.1f", MemoryMonitor.getMaxMemSpent()));
        StatWriter.log("memory-current-MB", String.format("%.1f", memoryUsed));
        StatWriter.log("max-depth-explored", String.format("%d", totalStats.getDepthStats().getDepth()));
        SearchLogger.log(String.format("Max Depth Explored       %d", totalStats.getDepthStats().getDepth()));

        // print solver statistics
        StatWriter.logSolverStats();
    }

    private PrimitiveVS<Machine> getNextSenderSummary(List<PrimitiveVS> candidateSenders) {
        PrimitiveVS<Machine> choices = (PrimitiveVS<Machine>) NondetUtil.getNondetChoice(candidateSenders);
        schedule.addRepeatSender(choices, choiceDepth);
        choiceDepth++;
        return choices;
    }

    protected List<PrimitiveVS> getNextSenderChoices() {
        // prioritize the create actions
        for (Machine machine : machines) {
            if (!machine.sendBuffer.isEmpty()) {
                Guard initCond = machine.sendBuffer.hasCreateMachineUnderGuard().getGuardFor(true);
                if (!initCond.isFalse()) {
                    PrimitiveVS<Machine> ret = new PrimitiveVS<>(machine).restrict(initCond);
                    return new ArrayList<>(Collections.singletonList(ret));
                }
            }
        }

        // prioritize the sync actions i.e. events that are marked as synchronous
        for (Machine machine : machines) {
            if (!machine.sendBuffer.isEmpty()) {
                Guard syncCond = machine.sendBuffer.hasSyncEventUnderGuard().getGuardFor(true);
                if (!syncCond.isFalse()) {
                    PrimitiveVS<Machine> ret = new PrimitiveVS<>(machine).restrict(syncCond);
                    return new ArrayList<>(Collections.singletonList(ret));
                }
            }
        }

        // now there are no create machine and sync event actions remaining
        List<GuardedValue<Machine>> guardedMachines = new ArrayList<>();

        for (Machine machine : machines) {
            if (!machine.sendBuffer.isEmpty()) {
                Guard canRun = machine.sendBuffer.satisfiesPredUnderGuard(x -> x.canRun()).getGuardFor(true);
                if (!canRun.isFalse()) {
                    guardedMachines.add(new GuardedValue(machine, canRun));
                }
            }
        }

        executionFinished = guardedMachines.stream().map(x -> x.getGuard().and(schedule.getFilter())).allMatch(x -> x.isFalse());

        List<PrimitiveVS> candidateSenders = new ArrayList<>();
        for (GuardedValue<Machine> guardedValue : guardedMachines) {
            candidateSenders.add(new PrimitiveVS<>(guardedValue.getValue()).restrict(guardedValue.getGuard()));
        }
        return candidateSenders;
    }

}
