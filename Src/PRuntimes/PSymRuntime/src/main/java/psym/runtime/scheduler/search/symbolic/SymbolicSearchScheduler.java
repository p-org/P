package psym.runtime.scheduler.search.symbolic;

import java.math.BigDecimal;
import java.time.Duration;
import java.time.Instant;
import java.util.*;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import lombok.Getter;
import org.apache.commons.lang3.StringUtils;
import psym.runtime.PSymGlobal;
import psym.runtime.Program;
import psym.runtime.logger.*;
import psym.runtime.machine.Machine;
import psym.runtime.machine.MachineLocalState;
import psym.runtime.machine.events.Message;
import psym.runtime.scheduler.search.SearchScheduler;
import psym.runtime.scheduler.search.explicit.StateCachingMode;
import psym.runtime.scheduler.search.symmetry.SymmetryMode;
import psym.runtime.statistics.CoverageStats;
import psym.runtime.statistics.SearchStats;
import psym.runtime.statistics.SolverStats;
import psym.utils.monitor.MemoryMonitor;
import psym.utils.monitor.TimeMonitor;
import psym.valuesummary.Guard;
import psym.valuesummary.GuardedValue;
import psym.valuesummary.PrimitiveVS;
import psym.valuesummary.ValueSummary;
import psym.valuesummary.solvers.SolverEngine;

public class SymbolicSearchScheduler extends SearchScheduler {
  private transient final TreeMap<Integer, ProtocolState> depthToCachedProtocolState = new TreeMap<>();

  public SymbolicSearchScheduler(Program p) {
    super(p);
    if (PSymGlobal.getConfiguration().getSchChoiceBound() == 1
        && PSymGlobal.getConfiguration().getDataChoiceBound() == 1) {
      throw new RuntimeException(
              "Error: symbolic strategy does not support both schedule and data choice bounds as 1. Use other strategies instead.");
    }
  }

  public static void cleanup() {
    SolverEngine.cleanupEngine();
  }

  @Override
  protected void step() throws TimeoutException {
    srcState.clear();
    allMachinesHalted = Guard.constFalse();

    int numMessages = 0;
    int numMessagesMerged = 0;
    int numMessagesExplored = 0;
    int numStates = 0;

    if (!PSymGlobal.getConfiguration().isIterative()
            && PSymGlobal.getConfiguration().getStateCachingMode() == StateCachingMode.Symbolic) {
      ProtocolState srcProtocolState = new ProtocolState(currentMachines);
      for (int i = depth; i >= 0; i--) {
        ProtocolState cachedProtocolState = depthToCachedProtocolState.get(i);
        if (cachedProtocolState != null) {
          Guard areEqual = srcProtocolState.symbolicEquals(cachedProtocolState);
          if (!areEqual.isFalse()) {
            done = done.or(areEqual);
            if (done.isTrue()) {
              terminalLivenessEnabled = false;
              return;
            }
//            break;
          }
        }
      }
      depthToCachedProtocolState.put(depth, srcProtocolState);
    }

    if (PSymGlobal.getConfiguration().getSymmetryMode() != SymmetryMode.None) {
      PSymGlobal.getSymmetryTracker().mergeAllSymmetryClasses();
    }

    if (PSymGlobal.getConfiguration().isUseBacktrack()) {
      storeSrcState();
      schedule.setSchedulerDepth(getDepth());
      schedule.setSchedulerChoiceDepth(getChoiceDepth());
      schedule.setSchedulerState(srcState, machineCounters);
      schedule.setSchedulerSymmetry();
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

    if (PSymGlobal.getConfiguration().isChoiceOrchestrationLearning()) {
      PSymGlobal.getChoiceLearningStats()
              .setProgramStateHash(this, PSymGlobal.getConfiguration().getChoiceLearningStateMode(), schedulingChoices);
    }

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
      depth++;
    }

    if (!PSymGlobal.getConfiguration().isIterative()
            && PSymGlobal.getConfiguration().getStateCachingMode() == StateCachingMode.Symbolic) {
      Message effectNew = effect.restrict(done.not());
      if (effectNew.isEmptyVS()) {
        return;
      }
      effect = effectNew;
    }

    TraceLogger.schedule(depth, effect);

    performEffect(effect);


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
  protected void printCurrentStatus(double newRuntime) {
    StringBuilder s = new StringBuilder(100);

    s.append("--------------------");
    s.append(String.format("\n    Status after %.2f seconds:", newRuntime));
    s.append(String.format("\n      Memory:           %.2f MB", MemoryMonitor.getMemSpent()));
    s.append(String.format("\n      Depth:            %d", getDepth()));

    if (PSymGlobal.getConfiguration().isIterative()) {
      s.append(String.format("\n      Progress:         %.12f",
              PSymGlobal.getCoverage().getEstimatedCoverage(12)));
      s.append(String.format("\n      Schedules:        %d", (getIter() - getStart_iter())));
      s.append(String.format("\n      Finished:         %d", getFinishedTasks().size()));
      s.append(String.format("\n      Remaining:        %d", getTotalNumBacktracks()));
    }

    ScratchLogger.log(s.toString());
  }

  @Override
  protected void printProgressHeader(boolean consolePrint) {
    StringBuilder s = new StringBuilder(100);
    s.append(StringUtils.center("Time", 11));
    s.append(StringUtils.center("Memory", 9));
    s.append(StringUtils.center("Depth", 7));

    if (PSymGlobal.getConfiguration().isIterative()) {
      s.append(StringUtils.center("Schedule", 12));
      s.append(StringUtils.center("Remaining", 24));
      s.append(StringUtils.center("Progress", 24));
    }

    if (consolePrint) {
      System.out.println(s);
    } else {
      PSymLogger.info(s.toString());
    }
  }

  @Override
  protected void printProgress(boolean forcePrint) {
    if (forcePrint
            || !PSymGlobal.getConfiguration().isIterative()
            || (TimeMonitor.getInstance().findInterval(getLastReportTime()) > 5)) {
      setLastReportTime(Instant.now());
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

        if (PSymGlobal.getConfiguration().isIterative()) {
          s.append(StringUtils.center(String.format("%d", (getIter() - getStart_iter())), 12));
          s.append(
              StringUtils.center(
                  String.format(
                      "%d (%.0f %% data)",
                      getTotalNumBacktracks(), getTotalDataBacktracksPercent()),
                  24));
          s.append(
              StringUtils.center(
                  String.format(
                      "%.12f (%s)",
                      PSymGlobal.getCoverage().getEstimatedCoverage(12),
                      PSymGlobal.getCoverage().getCoverageGoalAchieved()),
                  24));
        }

        if (consolePrint) {
          System.out.print(s);
        } else {
          SearchLogger.log(s.toString());
        }
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

  @Override
  public void reportEstimatedCoverage() {
    PSymGlobal.getCoverage().reportChoiceCoverage();

    BigDecimal coverage22 = PSymGlobal.getCoverage().getEstimatedCoverage(22);
    assert (coverage22.compareTo(BigDecimal.ONE) <= 0) : "Error in progress estimation";

    BigDecimal coverage12 = PSymGlobal.getCoverage().getEstimatedCoverage(12);

    String coverageGoalAchieved = PSymGlobal.getCoverage().getCoverageGoalAchieved();
    if (isFinalResult && PSymGlobal.getResult().equals("correct for any depth")) {
      PSymGlobal.getCoverage();
      coverage22 = CoverageStats.getMaxCoverage();
      coverage12 = CoverageStats.getMaxCoverage();
      coverageGoalAchieved = CoverageStats.getMaxCoverageGoal();
    }

    StatWriter.log("progress", String.format("%.22f", coverage22));
    StatWriter.log("coverage-achieved", String.format("%s", coverageGoalAchieved));

    if (PSymGlobal.getConfiguration().isIterative()) {
      SearchLogger.log(
              String.format(
                      "Progress Guarantee       %.12f", coverage12));
      SearchLogger.log(String.format("Coverage Goal Achieved   %s", coverageGoalAchieved));
    }
  }

  @Override
  protected void reset_stats() {
    searchStats.reset_stats();
    PSymGlobal.getCoverage().resetCoverage();
    if (PSymGlobal.getConfiguration().isChoiceOrchestrationLearning()) {
      PSymGlobal.getChoiceLearningStats()
              .setProgramStateHash(this, PSymGlobal.getConfiguration().getChoiceLearningStateMode(), null);
    }
    searchStats.reset_stats();
  }

  /** Reset scheduler state */
  @Override protected void reset() {
    super.reset();
    depthToCachedProtocolState.clear();
  }

  /** Restore scheduler state */
  @Override protected void restore(int d, int cd) {
    super.restore(d, cd);
    depthToCachedProtocolState.clear();
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
