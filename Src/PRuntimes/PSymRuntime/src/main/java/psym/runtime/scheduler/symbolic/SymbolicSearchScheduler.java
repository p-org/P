package psym.runtime.scheduler.symbolic;

import java.time.Duration;
import java.time.Instant;
import java.util.*;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.function.BiConsumer;
import java.util.function.Consumer;
import java.util.function.Function;
import java.util.function.Supplier;
import java.util.stream.Collectors;
import lombok.Getter;
import org.apache.commons.lang3.StringUtils;
import psym.runtime.PSymGlobal;
import psym.runtime.Program;
import psym.runtime.logger.*;
import psym.runtime.machine.Machine;
import psym.runtime.machine.MachineLocalState;
import psym.runtime.machine.events.Message;
import psym.runtime.scheduler.SearchScheduler;
import psym.runtime.scheduler.explicit.StateCachingMode;
import psym.runtime.scheduler.symmetry.SymmetryMode;
import psym.runtime.statistics.SearchStats;
import psym.runtime.statistics.SolverStats;
import psym.utils.Assert;
import psym.utils.monitor.MemoryMonitor;
import psym.utils.monitor.TimeMonitor;
import psym.valuesummary.Guard;
import psym.valuesummary.GuardedValue;
import psym.valuesummary.PrimitiveVS;
import psym.valuesummary.ValueSummary;
import psym.valuesummary.solvers.SolverEngine;

public class SymbolicSearchScheduler extends SearchScheduler {
  private final TreeMap<Integer, ProtocolState> depthToProtocolState = new TreeMap<>();

  public SymbolicSearchScheduler(Program p) {
    super(p);
  }

  public static void cleanup() {
    SolverEngine.cleanupEngine();
  }

  @Override
  public void doSearch() throws TimeoutException {
    result = "incomplete";
    SearchLogger.logStartExecution(1, getDepth());
    initializeSearch();
    if (PSymGlobal.getConfiguration().getVerbosity() == 0) {
      printProgressHeader(true);
    }
    searchStats.startNewIteration(1, 0);
    performSearch();
    summarizeIteration(0);
  }

  @Override
  public void resumeSearch() throws InterruptedException {
    throw new InterruptedException("Not implemented");
  }

  @Override
  public void performSearch() throws TimeoutException {
    while (!isDone()) {
      printProgress(false);
      Assert.prop(
          getDepth() < PSymGlobal.getConfiguration().getMaxStepBound(),
          "Maximum allowed depth " + PSymGlobal.getConfiguration().getMaxStepBound() + " exceeded",
          schedule.getLengthCond(schedule.size()));
      step();
      checkLiveness(allMachinesHalted);
    }
    checkLiveness(Guard.constTrue());
    Assert.prop(
        !PSymGlobal.getConfiguration().isFailOnMaxStepBound() || (getDepth() < PSymGlobal.getConfiguration().getMaxStepBound()),
        "Scheduling steps bound of " + PSymGlobal.getConfiguration().getMaxStepBound() + " reached.",
        schedule.getLengthCond(schedule.size()));
    if (done.isTrue()) {
      searchStats.setIterationCompleted();
    }
  }

  @Override
  protected void step() throws TimeoutException {
    allMachinesHalted = Guard.constFalse();

    int numStates = 0;
    int numMessages = 0;
    int numMessagesMerged = 0;
    int numMessagesExplored = 0;

    if (PSymGlobal.getConfiguration().getSymmetryMode() != SymmetryMode.None) {
      PSymGlobal.getSymmetryTracker().mergeAllSymmetryClasses();
    }

    removeHalted();

    PrimitiveVS<Machine> schedulingChoices = getNextSchedulingChoice();

    if (schedulingChoices.isEmptyVS()) {
      done = Guard.constTrue();
      SearchLogger.finishedExecution(depth);
    }

    if (done.isTrue()) {
      return;
    }

    SolverStats.checkResourceLimits();

    Message effect = null;
    List<Message> effects = new ArrayList<>();

    for (GuardedValue<Machine> schedulingChoice : schedulingChoices.getGuardedValues()) {
      Machine machine = schedulingChoice.getValue();
      Guard guard = schedulingChoice.getGuard();
      Message removed = rmBuffer(machine, guard);

      if (PSymGlobal.getConfiguration().getVerbosity() > 5) {
        System.out.println("  Machine " + machine);
        System.out.println("    state   " + machine.getCurrentState().toStringDetailed());
        System.out.println("    message " + removed.toString());
        System.out.println("    target " + removed.getTarget().toString());
      }
      if (effect == null) {
        effect = removed;
      } else {
        effects.add(removed);
      }
    }

    assert effect != null;
    effect = effect.merge(effects);

    stickyStep = false;
    if (effects.isEmpty()) {
      if (!effect.isCreateMachine().getGuardFor(true).isFalse()
          || !effect.isSyncEvent().getGuardFor(true).isFalse()) {
        stickyStep = true;
      }
    }
    if (!stickyStep) {
      if (PSymGlobal.getConfiguration().getStateCachingMode() == StateCachingMode.Exact) {
        effect = effect.restrict(done.not());
      }
      depth++;
    }

    TraceLogger.schedule(depth, effect);

    performEffect(effect);

    if (!stickyStep) {
      if (PSymGlobal.getConfiguration().getStateCachingMode() == StateCachingMode.Exact) {
        ProtocolState destProtocolState = new ProtocolState(currentMachines);
        for (ProtocolState srcProtocolState : depthToProtocolState.values()) {
          Guard areEqual = destProtocolState.symbolicEquals(srcProtocolState);
          done = done.or(areEqual);
          allMachinesHalted = allMachinesHalted.or(done);
        }
        depthToProtocolState.put(depth, destProtocolState);
      }
    }

    // simplify engine
    //        SolverEngine.simplifyEngineAuto();

    // switch engine
    //        SolverEngine.switchEngineAuto();

    double memoryUsed = MemoryMonitor.getMemSpent();
    if (memoryUsed > (0.8 * MemoryMonitor.getMemLimit())) {
      cleanup();
    }
    SolverStats.checkResourceLimits();

    // record depth statistics
    SearchStats.DepthStats depthStats =
        new SearchStats.DepthStats(
            depth, numStates, numMessages, numMessagesMerged, numMessagesExplored);
    searchStats.addDepthStatistics(depth, depthStats);

    // log statistics
    if (PSymGlobal.getConfiguration().getVerbosity() > 3) {
      double timeUsed = TimeMonitor.getInstance().getRuntime();
      if (PSymGlobal.getConfiguration().getVerbosity() > 4) {
        SearchLogger.log("--------------------");
        SearchLogger.log("Resource Stats::");
        SearchLogger.log("time-seconds", String.format("%.1f", timeUsed));
        SearchLogger.log("memory-max-MB", String.format("%.1f", MemoryMonitor.getMaxMemSpent()));
        SearchLogger.log("memory-current-MB", String.format("%.1f", memoryUsed));
        SearchLogger.log("--------------------");
        SearchLogger.log("Solver Stats::");
        SearchLogger.log(
            "time-create-guards-%",
            String.format(
                "%.1f",
                SolverStats.getDoublePercent(
                    SolverStats.timeTotalCreateGuards / 1000.0, timeUsed)));
        SearchLogger.log(
            "time-solve-guards-%",
            String.format(
                "%.1f",
                SolverStats.getDoublePercent(SolverStats.timeTotalSolveGuards / 1000.0, timeUsed)));
        SearchLogger.log(
            "time-create-guards-max-seconds",
            String.format("%.3f", SolverStats.timeMaxCreateGuards / 1000.0));
        SearchLogger.log(
            "time-solve-guards-max-seconds",
            String.format("%.3f", SolverStats.timeMaxSolveGuards / 1000.0));
        SolverStats.logSolverStats();
        SearchLogger.log("--------------------");
        SearchLogger.log("Detailed Solver Stats::");
        SearchLogger.log(SolverEngine.getStats());
        SearchLogger.log("--------------------");
      }
    }

    // log depth statistics
    if (PSymGlobal.getConfiguration().getVerbosity() > 4) {
      SearchLogger.logDepthStats(depthStats);
      System.out.println("--------------------");
      System.out.println("Collect Stats::");
      System.out.println(
          "Total transitions:: "
              + depthStats.getNumOfTransitions()
              + ", Total Merged Transitions (merged same target):: "
              + depthStats.getNumOfMergedTransitions()
              + ", Total Transitions Explored:: "
              + depthStats.getNumOfTransitionsExplored());
      System.out.println(
          "Running Total Transitions:: "
              + searchStats.getSearchTotal().getDepthStats().getNumOfTransitions()
              + ", Running Total Merged Transitions:: "
              + searchStats.getSearchTotal().getDepthStats().getNumOfMergedTransitions()
              + ", Running Total Transitions Explored:: "
              + searchStats.getSearchTotal().getDepthStats().getNumOfTransitionsExplored());
      System.out.println("--------------------");
    }
  }

  @Override
  protected PrimitiveVS getNext(
      int depth,
      Function<Integer, PrimitiveVS> getRepeat,
      Function<Integer, List> getBacktrack,
      Consumer<Integer> clearBacktrack,
      BiConsumer<PrimitiveVS, Integer> addRepeat,
      BiConsumer<List, Integer> addBacktrack,
      Supplier<List> getChoices,
      Function<List, PrimitiveVS> generateNext,
      boolean isData) {
    List<ValueSummary> choices = new ArrayList();
    if (depth < schedule.size()) {
      PrimitiveVS repeat = getRepeat.apply(depth);
      if (!repeat.getUniverse().isFalse()) {
        schedule.restrictFilterForDepth(depth);
        return repeat;
      }
      // nothing to repeat, so look at backtrack set
      choices = getBacktrack.apply(depth);
      clearBacktrack.accept(depth);
    }

    if (choices.isEmpty()) {
      // no choice to backtrack to, so generate new choices
      choices = getChoices.get();
      if (!isData && PSymGlobal.getConfiguration().getSymmetryMode() != SymmetryMode.None) {
        choices = PSymGlobal.getSymmetryTracker().getReducedChoices(choices);
      }
      choices =
          choices.stream()
              .map(x -> x.restrict(schedule.getFilter()))
              .filter(x -> !(x.getUniverse().isFalse()))
              .collect(Collectors.toList());
    }

    List<ValueSummary> chosen = choices;
    PrimitiveVS chosenVS = generateNext.apply(chosen);

    //        addRepeat.accept(chosenVS, depth);
    addBacktrack.accept(new ArrayList(), depth);
    schedule.restrictFilterForDepth(depth);

    return chosenVS;
  }

  @Override
  protected void summarizeIteration(int startDepth) {
    printProgress(false);
  }

  @Override
  protected void recordResult(SearchStats.TotalStats totalStats) {
    result = "";
    if (totalStats.isCompleted()) {
      result += "correct for any depth";
    } else {
      int safeDepth = PSymGlobal.getConfiguration().getMaxStepBound();
      if (totalStats.getDepthStats().getDepth() < safeDepth) {
        safeDepth = totalStats.getDepthStats().getDepth();
      }
      result += "correct up to step " + safeDepth;
    }
  }

  @Override
  protected void printCurrentStatus(double newRuntime) {
    String str =
        "--------------------"
            + String.format("\n    Status after %.2f seconds:", newRuntime)
            + String.format("\n      Memory:           %.2f MB", MemoryMonitor.getMemSpent())
            + String.format("\n      Depth:            %d", getDepth());
    ScratchLogger.log(str);
  }

  @Override
  protected void printProgressHeader(boolean consolePrint) {
    StringBuilder s = new StringBuilder(100);
    s.append(StringUtils.center("Time", 11));
    s.append(StringUtils.center("Memory", 9));
    s.append(StringUtils.center("Depth", 7));

    if (consolePrint) {
      System.out.println(s);
    } else {
      PSymLogger.info(s.toString());
    }
  }

  @Override
  protected void printProgress(boolean forcePrint) {
    double newRuntime = TimeMonitor.getInstance().getRuntime();
    printCurrentStatus(newRuntime);
    boolean consolePrint = (PSymGlobal.getConfiguration().getVerbosity() == 0);
    if (consolePrint || forcePrint) {
      long runtime = (long) (newRuntime * 1000);
      String runtimeHms =
          String.format(
              "%02d:%02d:%02d",
              TimeUnit.MILLISECONDS.toHours(runtime),
              TimeUnit.MILLISECONDS.toMinutes(runtime) % TimeUnit.HOURS.toMinutes(1),
              TimeUnit.MILLISECONDS.toSeconds(runtime) % TimeUnit.MINUTES.toSeconds(1));

      StringBuilder s = new StringBuilder(100);
      if (consolePrint) {
        s.append('\r');
      } else {
        PSymLogger.info("--------------------");
        printProgressHeader(false);
      }
      s.append(StringUtils.center(String.format("%s", runtimeHms), 11));
      s.append(
          StringUtils.center(String.format("%.1f GB", MemoryMonitor.getMemSpent() / 1024), 9));
      s.append(StringUtils.center(String.format("%d", getDepth()), 7));
      if (consolePrint) {
        System.out.print(s);
      } else {
        SearchLogger.log(s.toString());
      }
    }
  }

  @Override
  public void print_search_stats() {
    SearchStats.TotalStats totalStats = searchStats.getSearchTotal();
    double timeUsed =
        (Duration.between(TimeMonitor.getInstance().getStart(), Instant.now()).toMillis() / 1000.0);
    double memoryUsed = MemoryMonitor.getMemSpent();

    super.print_stats(totalStats, timeUsed, memoryUsed);

    StatWriter.log(
        "time-create-guards-%",
        String.format(
            "%.1f",
            SolverStats.getDoublePercent(SolverStats.timeTotalCreateGuards / 1000.0, timeUsed)));
    StatWriter.log(
        "time-solve-guards-%",
        String.format(
            "%.1f",
            SolverStats.getDoublePercent(SolverStats.timeTotalSolveGuards / 1000.0, timeUsed)));
    StatWriter.log(
        "time-create-guards-seconds",
        String.format("%.1f", SolverStats.timeTotalCreateGuards / 1000.0));
    StatWriter.log(
        "time-solve-guards-seconds",
        String.format("%.1f", SolverStats.timeTotalSolveGuards / 1000.0));
    StatWriter.log(
        "time-create-guards-max-seconds",
        String.format("%.3f", SolverStats.timeMaxCreateGuards / 1000.0));
    StatWriter.log(
        "time-solve-guards-max-seconds",
        String.format("%.3f", SolverStats.timeMaxSolveGuards / 1000.0));

    StatWriter.log(
        "#-events", String.format("%d", totalStats.getDepthStats().getNumOfTransitions()));
    StatWriter.log(
        "#-events-merged",
        String.format("%d", totalStats.getDepthStats().getNumOfMergedTransitions()));
    StatWriter.log(
        "#-events-explored",
        String.format("%d", totalStats.getDepthStats().getNumOfTransitionsExplored()));
  }

  public static class ProtocolState {
    @Getter
    Map<Machine, MachineLocalState> stateMap = null;

    public ProtocolState(Collection<Machine> machines) {
      stateMap = new HashMap<>();
      for (Machine m: machines) {
        stateMap.put(m, m.getMachineLocalState());
      }
    }

    public Guard symbolicEquals(ProtocolState rhs) {
      Map<Machine, MachineLocalState> rhsStateMap = rhs.getStateMap();
      if (stateMap.size() != rhsStateMap.size()) {
        return Guard.constFalse();
      }

      Guard areEqual = Guard.constTrue();
      for (Map.Entry<Machine, MachineLocalState> entry: stateMap.entrySet()) {
        Machine machine = entry.getKey();
        MachineLocalState lhsMachineState = entry.getValue();
        MachineLocalState rhsMachineState = rhsStateMap.get(machine);
        if (rhsMachineState == null) {
          return Guard.constFalse();
        }
        List<ValueSummary> lhsLocals = lhsMachineState.getLocals();
        List<ValueSummary> rhsLocals = rhsMachineState.getLocals();
        assert (lhsLocals.size() == rhsLocals.size());

        for (int i = 0; i < lhsLocals.size(); i++) {
          ValueSummary lhsVs = lhsLocals.get(i).restrict(areEqual);
          ValueSummary rhsVs = rhsLocals.get(i).restrict(areEqual);
          if (lhsVs.isEmptyVS() && rhsVs.isEmptyVS()) {
            continue;
          }
          areEqual = areEqual.and(lhsVs.symbolicEquals(rhsVs, Guard.constTrue()).getGuardFor(true));
          if (areEqual.isFalse()) {
            return Guard.constFalse();
          }
        }
      }
      return areEqual;
    }

  }
}
