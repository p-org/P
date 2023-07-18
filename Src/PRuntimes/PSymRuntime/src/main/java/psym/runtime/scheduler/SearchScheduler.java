package psym.runtime.scheduler;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.function.BiConsumer;
import java.util.function.Consumer;
import java.util.function.Function;
import java.util.function.Supplier;
import psym.runtime.Program;
import psym.runtime.logger.*;
import psym.runtime.machine.Machine;
import psym.runtime.statistics.SearchStats;
import psym.utils.monitor.MemoryMonitor;
import psym.utils.random.NondetUtil;
import psym.valuesummary.*;

/** Represents the search scheduler */
public abstract class SearchScheduler extends Scheduler {
  protected SearchScheduler(Program p) {
    super(p);
  }

  protected abstract PrimitiveVS getNext(
      int depth,
      Function<Integer, PrimitiveVS> getRepeat,
      Function<Integer, List> getBacktrack,
      Consumer<Integer> clearBacktrack,
      BiConsumer<PrimitiveVS, Integer> addRepeat,
      BiConsumer<List, Integer> addBacktrack,
      Supplier<List> getChoices,
      Function<List, PrimitiveVS> generateNext,
      boolean isData);

  protected abstract void summarizeIteration(int startDepth) throws InterruptedException;

  protected abstract void recordResult(SearchStats.TotalStats totalStats);

  protected abstract void printCurrentStatus(double newRuntime);

  protected abstract void printProgressHeader(boolean consolePrint);

  protected abstract void printProgress(boolean forcePrint);

  public abstract void print_search_stats();

  @Override
  public PrimitiveVS<Machine> getNextSchedulingChoice() {
    int depth = choiceDepth;
    PrimitiveVS<Machine> res =
        getNext(
            depth,
            schedule::getRepeatSchedulingChoice,
            schedule::getBacktrackSchedulingChoice,
            schedule::clearBacktrack,
            schedule::addRepeatSchedulingChoice,
            schedule::addBacktrackSchedulingChoice,
            this::getNextSchedulingChoices,
            this::getNextSchedulingChoiceSummary,
            false);
    choiceDepth = depth + 1;
    return res;
  }

  @Override
  public PrimitiveVS<Boolean> getNextBoolean(Guard pc) {
    int depth = choiceDepth;
    PrimitiveVS<Boolean> res =
        getNext(
            depth,
            schedule::getRepeatBool,
            schedule::getBacktrackBool,
            schedule::clearBacktrack,
            schedule::addRepeatBool,
            schedule::addBacktrackBool,
            () -> super.getNextBooleanChoices(pc),
            super::getNextBoolean,
            true);
    choiceDepth = depth + 1;
    return res;
  }

  @Override
  public PrimitiveVS<Integer> getNextInteger(PrimitiveVS<Integer> bound, Guard pc) {
    int depth = choiceDepth;
    PrimitiveVS<Integer> res =
        getNext(
            depth,
            schedule::getRepeatInt,
            schedule::getBacktrackInt,
            schedule::clearBacktrack,
            schedule::addRepeatInt,
            schedule::addBacktrackInt,
            () -> super.getNextIntegerChoices(bound, pc),
            super::getNextInteger,
            true);
    choiceDepth = depth + 1;
    return res;
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
    StatWriter.log(
        "max-depth-explored", String.format("%d", totalStats.getDepthStats().getDepth()));
    SearchLogger.log(
        String.format("Max Depth Explored       %d", totalStats.getDepthStats().getDepth()));

    // print solver statistics
    StatWriter.logSolverStats();
  }

  private PrimitiveVS<Machine> getNextSchedulingChoiceSummary(List<PrimitiveVS> candidates) {
    PrimitiveVS<Machine> choices =
        (PrimitiveVS<Machine>) NondetUtil.getNondetChoice(candidates);
    schedule.addRepeatSchedulingChoice(choices, choiceDepth);
    choiceDepth++;
    return choices;
  }

  protected List<PrimitiveVS> getNextSchedulingChoices() {
    // prioritize the create actions
    for (Machine machine : machines) {
      if (!machine.getEventBuffer().isEmpty()) {
        Guard initCond = machine.getEventBuffer().hasCreateMachineUnderGuard().getGuardFor(true);
        if (!initCond.isFalse()) {
          PrimitiveVS<Machine> ret = new PrimitiveVS<>(machine).restrict(initCond);
          return new ArrayList<>(Collections.singletonList(ret));
        }
      }
    }

    // prioritize the sync actions i.e. events that are marked as synchronous
    for (Machine machine : machines) {
      if (!machine.getEventBuffer().isEmpty()) {
        Guard syncCond = machine.getEventBuffer().hasSyncEventUnderGuard().getGuardFor(true);
        if (!syncCond.isFalse()) {
          PrimitiveVS<Machine> ret = new PrimitiveVS<>(machine).restrict(syncCond);
          return new ArrayList<>(Collections.singletonList(ret));
        }
      }
    }

    // now there are no create machine and sync event actions remaining
    List<GuardedValue<Machine>> guardedMachines = new ArrayList<>();

    allMachinesHalted = Guard.constTrue();
    for (Machine machine : machines) {
      if (!machine.getEventBuffer().isEmpty()) {
        Guard canRun =
            machine.getEventBuffer().satisfiesPredUnderGuard(x -> x.canRun()).getGuardFor(true);
        if (!canRun.isFalse()) {
          guardedMachines.add(new GuardedValue(machine, canRun));
        }
        allMachinesHalted = allMachinesHalted.and(canRun.not());
      }
    }

    List<PrimitiveVS> candidates = new ArrayList<>();
    for (GuardedValue<Machine> guardedValue : guardedMachines) {
      candidates.add(
          new PrimitiveVS<>(guardedValue.getValue()).restrict(guardedValue.getGuard()));
    }
    return candidates;
  }
}
