package psym.runtime.scheduler.search.explicit;

import java.io.*;
import java.math.BigDecimal;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.time.Duration;
import java.time.Instant;
import java.util.*;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import org.apache.commons.lang3.StringUtils;
import psym.runtime.Concretizer;
import psym.runtime.PSymGlobal;
import psym.runtime.Program;
import psym.runtime.logger.*;
import psym.runtime.machine.Machine;
import psym.runtime.machine.events.Message;
import psym.runtime.scheduler.Schedule;
import psym.runtime.scheduler.search.SearchScheduler;
import psym.runtime.scheduler.search.symmetry.SymmetryMode;
import psym.runtime.statistics.CoverageStats;
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

public class ExplicitSearchScheduler extends SearchScheduler {
  /** Total number of states */
  private int totalStateCount = 0;
  /** Total number of distinct states */
  private int totalDistinctStateCount = 0;
  /** Map of distinct concrete state to schedule when first visited */
  private transient Map<Object, Integer> distinctStates = new HashMap<>();
  /** Guard corresponding on distinct states at a step */
  private transient boolean isDistinctState = true;

  public ExplicitSearchScheduler(Program p) {
    super(p);
    assert (PSymGlobal.getConfiguration().getSchChoiceBound() == 1);
    assert (PSymGlobal.getConfiguration().getDataChoiceBound() == 1);
  }

  /**
   * Read scheduler state from a file
   *
   * @param readFromFile Name of the input file containing the scheduler state
   * @return A scheduler object
   * @throws Exception Throw error if reading fails
   */
  public static ExplicitSearchScheduler readFromFile(String readFromFile) throws RuntimeException {
    ExplicitSearchScheduler result;
    try {
      PSymLogger.info("... Reading program state from file " + readFromFile);
      FileInputStream fis;
      fis = new FileInputStream(readFromFile);
      ObjectInputStream ois = new ObjectInputStream(fis);
      result = (ExplicitSearchScheduler) ois.readObject();
      PSymGlobal.setInstance((PSymGlobal) ois.readObject());
      result.reinitialize();
      PSymLogger.info("... Successfully read.");
    } catch (IOException | ClassNotFoundException e) {
      //      e.printStackTrace();
      throw new RuntimeException("... Failed to read program state from file " + readFromFile, e);
    }
    return result;
  }

  @Override
  protected void step() throws TimeoutException {
    srcState.clear();
    allMachinesHalted = Guard.constFalse();

    int numMessages = 0;
    int numMessagesMerged = 0;
    int numMessagesExplored = 0;
    int numStates = 0;
    int numStatesDistinct = 0;

    if (PSymGlobal.getConfiguration().getStateCachingMode() != StateCachingMode.None) {
      storeSrcState();
      int[] numConcrete = enumerateConcreteStatesFromExplicit(PSymGlobal.getConfiguration().getStateCachingMode());
      numStates = numConcrete[0];
      numStatesDistinct = numConcrete[1];

      if (!isDistinctState) {
        int firstVisitIter = numConcrete[2];
        if (firstVisitIter == getIter()) {
          if (PSymGlobal.getConfiguration().isFailOnMaxStepBound()) {
            Assert.cycle(
                    false,
                    "Cycle detected: Infinite loop found due to revisiting a state multiple times in the same schedule",
                    Guard.constTrue());
          }
        } else {
          // early termination (without cycle)
          terminalLivenessEnabled = false;
        }
        done = Guard.constTrue();
        SearchLogger.finishedExecution(depth);
        return;
      }
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

    List<GuardedValue<Machine>> schedulingChoicesGv = schedulingChoices.getGuardedValues();
    assert (schedulingChoicesGv.size() == 1);

    GuardedValue<Machine> schedulingChoice = schedulingChoicesGv.get(0);
    Machine machine = schedulingChoice.getValue();
    Guard guard = schedulingChoice.getGuard();
    Message removed = rmBuffer(machine, guard);

    if (PSymGlobal.getConfiguration().getVerbosity() > 5) {
      System.out.println("  Machine " + machine);
      System.out.println("    state   " + machine.getCurrentState().toStringDetailed());
      System.out.println("    message " + removed.toString());
      System.out.println("    target " + removed.getTarget().toString());
    }
    Message effect = removed;
    assert effect != null;

    stickyStep = false;
    if (!effect.isCreateMachine().getGuardFor(true).isFalse()
        || !effect.isSyncEvent().getGuardFor(true).isFalse()) {
      stickyStep = true;
    }
    if (!stickyStep) {
      depth++;
    }

    TraceLogger.schedule(depth, effect, machine);

    performEffect(effect);

    // simplify engine
    //        SolverEngine.simplifyEngineAuto();

    // switch engine
    //        SolverEngine.switchEngineAuto();

    double memoryUsed = MemoryMonitor.getMemSpent();

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
          "Total States:: " + numStates + ", Running Total States::" + totalStateCount);
      System.out.println(
          "Total Distinct States:: "
              + numStatesDistinct
              + ", Running Total Distinct States::"
              + totalDistinctStateCount);
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

    s.append(String.format("\n      Progress:         %.12f",
            PSymGlobal.getCoverage().getEstimatedCoverage(12)));
    s.append(String.format("\n      Schedules:        %d", (getIter() - getStart_iter())));
    s.append(String.format("\n      Finished:         %d", getFinishedTasks().size()));
    s.append(String.format("\n      Remaining:        %d", getTotalNumBacktracks()));

    if (PSymGlobal.getConfiguration().getStateCachingMode() != StateCachingMode.None) {
      s.append(String.format("\n      States:           %d", totalStateCount));
      s.append(String.format("\n      DistinctStates:   %d", totalDistinctStateCount));
    }

    ScratchLogger.log(s.toString());
  }

  @Override
  protected void printProgressHeader(boolean consolePrint) {
    StringBuilder s = new StringBuilder(100);
    s.append(StringUtils.center("Time", 11));
    s.append(StringUtils.center("Memory", 9));
    s.append(StringUtils.center("Depth", 7));

    s.append(StringUtils.center("Schedule", 12));
    s.append(StringUtils.center("Remaining", 24));
    s.append(StringUtils.center("Progress", 24));

    if (PSymGlobal.getConfiguration().getStateCachingMode() != StateCachingMode.None) {
      s.append(StringUtils.center("States", 12));
    }

    if (consolePrint) {
      System.out.println(s);
    } else {
      PSymLogger.info(s.toString());
    }
  }

  @Override
  protected void printProgress(boolean forcePrint) {
    if (forcePrint || (TimeMonitor.getInstance().findInterval(getLastReportTime()) > 5)) {
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

        s.append(StringUtils.center(String.format("%d", (getIter() - getStart_iter())), 12));
        s.append(
            StringUtils.center(
                String.format(
                    "%d (%.0f %% data)", getTotalNumBacktracks(), getTotalDataBacktracksPercent()),
                24));
        s.append(
            StringUtils.center(
                String.format(
                    "%.12f (%s)",
                    PSymGlobal.getCoverage().getEstimatedCoverage(12),
                    PSymGlobal.getCoverage().getCoverageGoalAchieved()),
                24));

        if (PSymGlobal.getConfiguration().getStateCachingMode() != StateCachingMode.None) {
          s.append(StringUtils.center(String.format("%d", totalDistinctStateCount), 12));
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

    // print states statistics
    StatWriter.log("#-states", String.format("%d", totalStateCount));
    StatWriter.log("#-distinct-states", String.format("%d", totalDistinctStateCount));

    // print symmetry statistics
    StatWriter.log("#-pruned-symmetry", String.format("%d", ExplicitSymmetryTracker.getPruneCount()));
  }

  @Override
  public void reportEstimatedCoverage() {
    PSymGlobal.getCoverage().reportChoiceCoverage();

    if (PSymGlobal.getConfiguration().getStateCachingMode() != StateCachingMode.None) {
      SearchLogger.log(String.format("Distinct States Explored %d", totalDistinctStateCount));
    }

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

    SearchLogger.log(
        String.format(
            "Progress Guarantee       %.12f", coverage12));
    SearchLogger.log(String.format("Coverage Goal Achieved   %s", coverageGoalAchieved));
  }

  private String globalStateString() {
    StringBuilder out = new StringBuilder();
    out.append("Src State:").append(System.lineSeparator());
    for (Machine machine : currentMachines) {
      List<ValueSummary> machineLocalState = machine.getMachineLocalState().getLocals();
      out.append(String.format("  Machine: %s", machine)).append(System.lineSeparator());
      for (ValueSummary vs : machineLocalState) {
        out.append(String.format("    %s", vs.toStringDetailed())).append(System.lineSeparator());
      }
    }
    return out.toString();
  }

  private String getConcreteStateString(List<List<Object>> concreteState) {
    StringBuilder out = new StringBuilder();
    out.append(String.format("#%d[", concreteState.size()));
    //        out.append(System.lineSeparator());
    int i = 0;
    for (Machine m : currentMachines) {
      out.append("  ");
      out.append(m.toString());
      out.append(" -> ");
      out.append(concreteState.get(i).toString());
      i++;
      //            out.append(System.lineSeparator());
    }
    out.append("]");
    return out.toString();
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
    distinctStates.clear();
    totalStateCount = 0;
    totalDistinctStateCount = 0;
  }

  /** Reinitialize scheduler */
  private void reinitialize() {
    // set all transient data structures
    srcState = new HashMap<>();
    distinctStates = new HashMap<>();
    isDistinctState = true;
    for (Machine machine : schedule.getMachines()) {
      machine.setScheduler(this);
    }
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
      oos.writeObject(PSymGlobal.getInstance());
      if (PSymGlobal.getConfiguration().getVerbosity() > 0) {
        long szBytes = Files.size(Paths.get(writeFileName));
        PSymLogger.info(
            String.format("  %,.1f MB  written in %s", (szBytes / 1024.0 / 1024.0), writeFileName));
      }
    } catch (IOException e) {
      //      e.printStackTrace();
      throw new RuntimeException("Failed to write state in file " + writeFileName, e);
    }
  }

  /**
   * Write each backtracking point state individually
   *
   * @param prefix Output file name prefix
   * @throws Exception Throw error if writing fails
   */
  public void writeBacktracksToFiles(String prefix) throws Exception {
    for (int i = 0; i < schedule.size(); i++) {
      Schedule.Choice choice = schedule.getChoice(i);
      // if choice at this depth is non-empty
      if (choice.isBacktrackNonEmpty()) {
        writeBacktrackToFile(prefix, i);
      }
    }
    while (setNextBacktrackTask() != null) {
      for (int i = 0; i < schedule.size(); i++) {
        Schedule.Choice choice = schedule.getChoice(i);
        // if choice at this depth is non-empty
        if (choice.isBacktrackNonEmpty()) {
          writeBacktrackToFile(prefix, i);
        }
      }
    }
  }

  /**
   * Write backtracking point state individually at a given depth
   *
   * @param prefix Output file name prefix
   * @param backtrackChoiceDepth Backtracking point choice depth
   * @throws Exception Throw error if writing fails
   */
  public void writeBacktrackToFile(String prefix, int backtrackChoiceDepth) throws Exception {
    // create a copy of original choices
    List<Schedule.Choice> originalChoices = clearAndReturnOriginalTask(backtrackChoiceDepth);
    // clear the complete choice information (including repeats and backtracks) at all successor
    // depths
    for (int i = backtrackChoiceDepth + 1; i < schedule.size(); i++) {
      schedule.clearChoice(i);
    }
    int depth = schedule.getChoice(backtrackChoiceDepth).getSchedulerDepth();
    long pid = ProcessHandle.current().pid();
    String writeFileName =
        prefix
            + "_d"
            + depth
            + "_cd"
            + backtrackChoiceDepth
            + "_task"
            + getLatestTaskId()
            + "_pid"
            + pid
            + ".out";
    // write to file
    writeToFile(writeFileName);
    BacktrackWriter.log(
        writeFileName,
        PSymGlobal.getCoverage().getPathCoverageAtDepth(backtrackChoiceDepth),
        depth,
        backtrackChoiceDepth);

    // restore schedule to original choices
    schedule.setChoices(originalChoices);
  }

  public void writeToFile() throws Exception {
    if (PSymGlobal.getConfiguration().getVerbosity() > 0) {
      PSymLogger.info(
          String.format(
              "Writing 1 current and %d backtrack states in %s/",
              getTotalNumBacktracks(), PSymGlobal.getConfiguration().getOutputFolder()));
    }
    long pid = ProcessHandle.current().pid();
    String writeFileName = PSymGlobal.getConfiguration().getOutputFolder() + "/current" + "_pid" + pid + ".out";
    writeToFile(writeFileName);
    writeBacktracksToFiles(PSymGlobal.getConfiguration().getOutputFolder() + "/backtrack");
    if (PSymGlobal.getConfiguration().getVerbosity() > 0) {
      PSymLogger.info("--------------------");
    }
  }

  /**
   * Enumerate concrete states from explicit
   *
   * @return number of concrete states represented by the symbolic state
   */
  public int[] enumerateConcreteStatesFromExplicit(StateCachingMode mode) {
    if (PSymGlobal.getConfiguration().getVerbosity() > 5) {
      PSymLogger.info(globalStateString());
    }

    if (stickyStep || (choiceDepth <= getBacktrackDepth()) || (mode == StateCachingMode.None)) {
      isDistinctState = true;
      return new int[] {0, 0, -1};
    }

    List<List<Object>> globalStateConcrete = new ArrayList<>();
    for (Machine m : currentMachines) {
      assert (srcState.containsKey(m));
      List<ValueSummary> machineStateSymbolic = srcState.get(m).getLocals();
      List<Object> machineStateConcrete = new ArrayList<>();
      for (int j = 0; j < machineStateSymbolic.size(); j++) {
        Object varValue = null;
        if (mode == StateCachingMode.ExplicitFast) {
          varValue = machineStateSymbolic.get(j).getConcreteHash();
        } else {
          GuardedValue<?> guardedValue = Concretizer.concretize(machineStateSymbolic.get(j));
          if (guardedValue != null) {
            varValue = guardedValue.getValue();
          }
        }
        machineStateConcrete.add(varValue);
      }
      globalStateConcrete.add(machineStateConcrete);
    }

    String concreteState = globalStateConcrete.toString();
    totalStateCount += 1;
    if (distinctStates.containsKey(concreteState)) {
      if (PSymGlobal.getConfiguration().getVerbosity() > 5) {
        PSymLogger.info("Repeated State: " + getConcreteStateString(globalStateConcrete));
      }
      isDistinctState = false;
      return new int[] {1, 0, distinctStates.get(concreteState)};
    } else {
      if (PSymGlobal.getConfiguration().getVerbosity() > 4) {
        PSymLogger.info("New State:      " + getConcreteStateString(globalStateConcrete));
      }
      distinctStates.put(concreteState, getIter());
      totalDistinctStateCount += 1;
      isDistinctState = true;
      return new int[] {1, 1, -1};
    }
  }
}
