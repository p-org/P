package psym.runtime.scheduler.explicit;

import java.io.*;
import java.math.BigDecimal;
import java.nio.file.Files;
import java.nio.file.Paths;
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
import psym.runtime.Concretizer;
import psym.runtime.PSymGlobal;
import psym.runtime.Program;
import psym.runtime.logger.*;
import psym.runtime.machine.Machine;
import psym.runtime.machine.MachineLocalState;
import psym.runtime.machine.events.Message;
import psym.runtime.scheduler.Schedule;
import psym.runtime.scheduler.SearchScheduler;
import psym.runtime.scheduler.explicit.choiceorchestration.*;
import psym.runtime.scheduler.explicit.taskorchestration.TaskOrchestrationMode;
import psym.runtime.scheduler.symmetry.SymmetryMode;
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
  /** List of all backtrack tasks */
  private final List<BacktrackTask> allTasks = new ArrayList<>();
  /** Priority queue of all backtrack tasks that are pending */
  private final Set<Integer> pendingTasks = new HashSet<>();
  /** List of all backtrack tasks that finished */
  private final List<Integer> finishedTasks = new ArrayList<>();

  private final ChoiceOrchestrator choiceOrchestrator;
  protected boolean isDoneIterating = false;
  /** Source state at the beginning of each schedule step */
  protected transient Map<Machine, MachineLocalState> srcState = new HashMap<>();

  @Getter int iter = 0;
  @Getter int start_iter = 0;
  /** Backtrack choice depth */
  int backtrackDepth = 0;
  /** Task id of the latest backtrack task */
  private int latestTaskId = 0;

  private int numPendingBacktracks = 0;
  private int numPendingDataBacktracks = 0;
  /** Map of distinct concrete state to iteration when first visited */
  private transient Map<Object, Integer> distinctStates = new HashMap<>();
  /** Guard corresponding on distinct states at a step */
  private transient boolean isDistinctState = true;
  /** Total number of states */
  private int totalStateCount = 0;
  /** Total number of distinct states */
  private int totalDistinctStateCount = 0;
  /** Time of last report */
  private transient Instant lastReportTime = Instant.now();


  public ExplicitSearchScheduler(Program p) {
    super(p);
    switch (PSymGlobal.getConfiguration().getChoiceOrchestration()) {
      case None:
        choiceOrchestrator = new ChoiceOrchestratorNone();
        break;
      case Random:
        choiceOrchestrator = new ChoiceOrchestratorRandom();
        break;
      case QLearning:
        choiceOrchestrator = new ChoiceOrchestratorQLearning();
        break;
      case EpsilonGreedy:
        choiceOrchestrator = new ChoiceOrchestratorEpsilonGreedy();
        break;
      default:
        throw new RuntimeException(
            "Unrecognized choice orchestration mode: " + PSymGlobal.getConfiguration().getChoiceOrchestration());
    }
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
  public void doSearch() throws TimeoutException, InterruptedException {
    resetBacktrackTasks();
    boolean initialRun = true;
    result = "incomplete";
    iter++;
    SearchLogger.logStartExecution(iter, getDepth());
    initializeSearch();
    if (PSymGlobal.getConfiguration().getVerbosity() == 0) {
      printProgressHeader(true);
    }
    while (!isDoneIterating) {
      if (initialRun) {
        initialRun = false;
      } else {
        iter++;
        SearchLogger.logStartExecution(iter, getDepth());
      }
      searchStats.startNewIteration(iter, backtrackDepth);
      performSearch();
      summarizeIteration(backtrackDepth);
    }
  }

  @Override
  public void resumeSearch() throws TimeoutException, InterruptedException {
    resetBacktrackTasks();
    boolean initialRun = true;
    isDoneIterating = false;
    start_iter = iter;
    reset_stats();
    schedule.setNumBacktracksInSchedule();
    boolean resetAfterInitial = isDone();
    if (PSymGlobal.getConfiguration().getVerbosity() == 0) {
      printProgressHeader(true);
    }
    while (!isDoneIterating) {
      if (initialRun) {
        initialRun = false;
        SearchLogger.logResumeExecution(iter, getDepth());
      } else {
        iter++;
        SearchLogger.logStartExecution(iter, getDepth());
      }
      searchStats.startNewIteration(iter, backtrackDepth);
      performSearch();
      summarizeIteration(backtrackDepth);
      if (resetAfterInitial) {
        resetAfterInitial = false;
        PSymGlobal.getCoverage().resetCoverage();
      }
    }
  }

  @Override
  protected void performSearch() throws TimeoutException {
    schedule.setNumBacktracksInSchedule();
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
    schedule.setNumBacktracksInSchedule();
    if (done.isTrue()) {
      searchStats.setIterationCompleted();
    }
  }

  @Override
  protected void step() throws TimeoutException {
    srcState.clear();
    allMachinesHalted = Guard.constFalse();

    int numStates = 0;
    int numStatesDistinct = 0;
    int numMessages = 0;
    int numMessagesMerged = 0;
    int numMessagesExplored = 0;

    if (PSymGlobal.getConfiguration().getStateCachingMode() != StateCachingMode.None) {
      storeSrcState();
      int[] numConcrete = enumerateConcreteStatesFromExplicit(PSymGlobal.getConfiguration().getStateCachingMode());
      numStates = numConcrete[0];
      numStatesDistinct = numConcrete[1];

      if (!isDistinctState) {
        int firstVisitIter = numConcrete[2];
        if (firstVisitIter == iter) {
          allMachinesHalted = Guard.constTrue();
//          Assert.liveness(
//                  false,
//                  String.format("Cycle detected: Possible infinite loop found due to revisiting a state multiple times in the same iteration"),
//                  Guard.constTrue());
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

    Message effect = null;
    List<Message> effects = new ArrayList<>();

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
    if (effect == null) {
      effect = removed;
    } else {
      effects.add(removed);
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

    TraceLogger.schedule(depth, effect);

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
    boolean isNewChoice = false;

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
      if (getIter() > 0) SearchLogger.logMessage("new choice at depth " + depth);
      choices = getChoices.get();
      if (!isData && PSymGlobal.getConfiguration().getSymmetryMode() != SymmetryMode.None) {
        choices = PSymGlobal.getSymmetryTracker().getReducedChoices(choices);
      }
      choices =
          choices.stream()
              .map(x -> x.restrict(schedule.getFilter()))
              .filter(x -> !(x.getUniverse().isFalse()))
              .collect(Collectors.toList());
      isNewChoice = true;
    }

    if (choices.size() > 1) {
      choiceOrchestrator.reorderChoices(choices, isData);
    }

    List<ValueSummary> chosen = new ArrayList();
    ChoiceQTable.ChoiceQStateKey chosenQStateKey = new ChoiceQTable.ChoiceQStateKey();
    List<ValueSummary> backtrack = new ArrayList();
    for (int i = 0; i < choices.size(); i++) {
      ValueSummary choice = choices.get(i);
      if (i == 0) {
        chosen.add(choice);
        chosenQStateKey.add(choice);
      } else {
        backtrack.add(choice);
      }
    }
    ChoiceQTable.ChoiceQTableKey chosenActions = null;
    if (PSymGlobal.getConfiguration().isChoiceOrchestrationLearning()) {
      chosenActions =
          new ChoiceQTable.ChoiceQTableKey(
              PSymGlobal.getChoiceLearningStats().getProgramStateHash(), chosenQStateKey);
    }
    PSymGlobal.getCoverage()
        .updateDepthCoverage(
            getDepth(),
            getChoiceDepth(),
            chosen.size(),
            backtrack.size(),
            isData,
            isNewChoice,
            chosenActions);

    PrimitiveVS chosenVS = generateNext.apply(chosen);

    //        addRepeat.accept(chosenVS, depth);
    addBacktrack.accept(backtrack, depth);
    schedule.restrictFilterForDepth(depth);

    return chosenVS;
  }

  @Override
  protected void summarizeIteration(int startDepth) throws InterruptedException {
    if (PSymGlobal.getConfiguration().getVerbosity() > 3) {
      SearchLogger.logIterationStats(searchStats.getIterationStats().get(getIter()));
    }
    if (PSymGlobal.getConfiguration().getMaxExecutions() > 0) {
      isDoneIterating = ((getIter() - getStart_iter()) >= PSymGlobal.getConfiguration().getMaxExecutions());
    }
    PSymGlobal.getCoverage()
        .updateIterationCoverage(
            getChoiceDepth() - 1, startDepth, PSymGlobal.getConfiguration().getChoiceLearningRewardMode());
    if (PSymGlobal.getConfiguration().getTaskOrchestration() != TaskOrchestrationMode.DepthFirst) {
      setBacktrackTasks();
      BacktrackTask nextTask = setNextBacktrackTask();
      if (nextTask != null) {
        if (PSymGlobal.getConfiguration().getVerbosity() > 1) {
          PSymLogger.info(
              String.format(
                  "    Next is %s [depth: %d, parent: %s]",
                  nextTask, nextTask.getDepth(), nextTask.getParentTask()));
        }
      }
    }
    printProgress(false);
    if (!isDoneIterating) {
      postIterationCleanup();
    }
  }

  @Override
  protected void recordResult(SearchStats.TotalStats totalStats) {
    result = "";
    if (getStart_iter() != 0) {
      result += "(resumed run) ";
    }
    if (totalStats.isCompleted()) {
      if (getTotalNumBacktracks() == 0) {
        result += "correct for any depth";
      } else {
        result += "partially correct with " + getTotalNumBacktracks() + " backtracks remaining";
      }
    } else {
      int safeDepth = PSymGlobal.getConfiguration().getMaxStepBound();
      if (totalStats.getDepthStats().getDepth() < safeDepth) {
        safeDepth = totalStats.getDepthStats().getDepth();
      }
      if (getTotalNumBacktracks() == 0) {
        result += "correct up to step " + safeDepth;
      } else {
        result +=
            "partially correct up to step "
                + (PSymGlobal.getConfiguration().getMaxStepBound() - 1)
                + " with "
                + getTotalNumBacktracks()
                + " backtracks remaining";
      }
    }
  }

  @Override
  protected void printCurrentStatus(double newRuntime) {
    String str =
        "--------------------"
            + String.format("\n    Status after %.2f seconds:", newRuntime)
            + String.format("\n      Memory:           %.2f MB", MemoryMonitor.getMemSpent())
            + String.format("\n      Depth:            %d", getDepth())
            + String.format("\n      Progress:         %.12f",
                PSymGlobal.getCoverage().getEstimatedCoverage(12))
            + String.format("\n      Iterations:       %d", (getIter() - getStart_iter()))
            + String.format("\n      Finished:         %d", finishedTasks.size())
            + String.format("\n      Remaining:        %d", getTotalNumBacktracks())
            + String.format("\n      States:           %d", totalStateCount)
            + String.format("\n      DistinctStates:   %d", totalDistinctStateCount);
    ScratchLogger.log(str);
  }

  @Override
  protected void printProgressHeader(boolean consolePrint) {
    StringBuilder s = new StringBuilder(100);
    s.append(StringUtils.center("Time", 11));
    s.append(StringUtils.center("Memory", 9));
    s.append(StringUtils.center("Depth", 7));
    s.append(StringUtils.center("Iteration", 12));
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
    if (forcePrint || (TimeMonitor.getInstance().findInterval(lastReportTime) > 5)) {
      lastReportTime = Instant.now();
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

    // print learn statistics
    StatWriter.log(
        "learn-#-qstates", String.format("%d", PSymGlobal.getChoiceLearningStats().numQStates()));
    StatWriter.log(
        "learn-#-qvalues", String.format("%d", PSymGlobal.getChoiceLearningStats().numQValues()));

    // print task statistics
    StatWriter.log("#-tasks-finished", String.format("%d", finishedTasks.size()));
    StatWriter.log(
        "#-tasks-remaining", String.format("%d", (allTasks.size() - finishedTasks.size())));
    StatWriter.log("#-backtracks", String.format("%d", getTotalNumBacktracks()));
    StatWriter.log("%-backtracks-data", String.format("%.2f", getTotalDataBacktracksPercent()));
    StatWriter.log("#-executions", String.format("%d", (getIter() - getStart_iter())));
  }

  private void postIterationCleanup() {
    schedule.resetFilter();
    for (int d = schedule.size() - 1; d >= 0; d--) {
      Schedule.Choice choice = schedule.getChoice(d);
      choice.updateHandledUniverse(choice.getRepeatUniverse());
      schedule.clearRepeat(d);
      if (choice.isBacktrackNonEmpty()) {
        int newDepth = 0;
        if (PSymGlobal.getConfiguration().isUseBacktrack()) {
          newDepth = choice.getSchedulerDepth();
        }
        if (newDepth == 0) {
          for (Machine machine : machines) {
            machine.reset();
          }
        } else {
          restoreState(choice.getChoiceState());
          schedule.setFilter(choice.getFilter());
          if (PSymGlobal.getConfiguration().getSymmetryMode() != SymmetryMode.None) {
            PSymGlobal.setSymmetryTracker(choice.getSymmetry());
          }
        }
        SearchLogger.logMessage("backtrack to " + d);
        backtrackDepth = d;
        if (newDepth == 0) {
          reset();
          initializeSearch();
        } else {
          restore(newDepth, choice.getSchedulerChoiceDepth());
        }
        return;
      } else {
        schedule.clearChoice(d);
        PSymGlobal.getCoverage().resetPathCoverage(d);
      }
    }
    isDoneIterating = true;
  }

  /**
   * Estimates and prints a coverage percentage based on number of choices explored versus remaining
   * at each depth
   */
  public void reportEstimatedCoverage() {
    PSymGlobal.getCoverage().reportChoiceCoverage();

    if (PSymGlobal.getConfiguration().getStateCachingMode() != StateCachingMode.None) {
      SearchLogger.log(String.format("Distinct States Explored %d", totalDistinctStateCount));
    }

    BigDecimal coverage = PSymGlobal.getCoverage().getEstimatedCoverage(22);
    assert (coverage.compareTo(BigDecimal.ONE) <= 0) : "Error in progress estimation";

    String coverageGoalAchieved = PSymGlobal.getCoverage().getCoverageGoalAchieved();
    if (isFinalResult && result.endsWith("correct for any depth")) {
      PSymGlobal.getCoverage();
      coverageGoalAchieved = CoverageStats.getMaxCoverageGoal();
    }

    StatWriter.log("progress", String.format("%.22f", coverage));
    StatWriter.log("coverage-achieved", String.format("%s", coverageGoalAchieved));

    SearchLogger.log(
        String.format(
            "Progress Guarantee       %.12f", PSymGlobal.getCoverage().getEstimatedCoverage(12)));
    SearchLogger.log(String.format("Coverage Goal Achieved   %s", coverageGoalAchieved));
  }

  private void resetBacktrackTasks() {
    pendingTasks.clear();
    numPendingBacktracks = 0;
    numPendingDataBacktracks = 0;
    BacktrackTask.initialize(PSymGlobal.getConfiguration().getTaskOrchestration());
  }

  private void isValidTaskId(int taskId) {
    assert (taskId < allTasks.size());
  }

  public int getTotalNumBacktracks() {
    int count = schedule.getNumBacktracksInSchedule();
    count += numPendingBacktracks;
    return count;
  }

  public double getTotalDataBacktracksPercent() {
    int totalBacktracks = getTotalNumBacktracks();
    if (totalBacktracks == 0) {
      return 0.0;
    }
    int count = schedule.getNumDataBacktracksInSchedule();
    count += numPendingDataBacktracks;
    return (count * 100.0) / totalBacktracks;
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

  private BacktrackTask getTask(int taskId) {
    isValidTaskId(taskId);
    return allTasks.get(taskId);
  }

  private void setBacktrackTasks() {
    BacktrackTask parentTask;
    if (latestTaskId == 0) {
      assert (allTasks.isEmpty());
      BacktrackTask.setOrchestration(PSymGlobal.getConfiguration().getTaskOrchestration());
      parentTask = new BacktrackTask(0);
      parentTask.setPrefixCoverage(new BigDecimal(1));
      allTasks.add(parentTask);
    } else {
      parentTask = getTask(latestTaskId);
    }
    parentTask.postProcess(PSymGlobal.getCoverage().getPathCoverageAtDepth(getChoiceDepth() - 1));
    finishedTasks.add(parentTask.getId());
    if (PSymGlobal.getConfiguration().getVerbosity() > 1) {
      PSymLogger.info(
          String.format(
              "  Finished %s [depth: %d, parent: %s]",
              parentTask, parentTask.getDepth(), parentTask.getParentTask()));
    }

    int numBacktracksAdded = 0;
    for (int i = 0; i < schedule.size(); i++) {
      Schedule.Choice choice = schedule.getChoice(i);
      // if choice at this depth is non-empty
      if (choice.isBacktrackNonEmpty()) {
        if (PSymGlobal.getConfiguration().getMaxBacktrackTasksPerExecution() > 0
            && numBacktracksAdded == (PSymGlobal.getConfiguration().getMaxBacktrackTasksPerExecution() - 1)) {
          setBacktrackTaskAtDepthCombined(parentTask, i);
          numBacktracksAdded++;
          break;
        } else {
          // top backtrack should be never combined
          setBacktrackTaskAtDepthExact(parentTask, i);
          numBacktracksAdded++;
        }
      }
    }

    if (PSymGlobal.getConfiguration().getVerbosity() > 1) {
      PSymLogger.info(String.format("    Added %d new tasks", parentTask.getChildren().size()));
      if (PSymGlobal.getConfiguration().getVerbosity() > 2) {
        for (BacktrackTask t : parentTask.getChildren()) {
          PSymLogger.info(String.format("      %s [depth: %d]", t, t.getDepth()));
        }
      }
    }
  }

  private void setBacktrackTaskAtDepthExact(BacktrackTask parentTask, int backtrackChoiceDepth) {
    setBacktrackTaskAtDepth(parentTask, backtrackChoiceDepth, true);
  }

  private void setBacktrackTaskAtDepthCombined(BacktrackTask parentTask, int backtrackChoiceDepth) {
    setBacktrackTaskAtDepth(parentTask, backtrackChoiceDepth, false);
  }

  private List<Schedule.Choice> clearAndReturnOriginalTask(int backtrackChoiceDepth) {
    // create a copy of original choices
    List<Schedule.Choice> originalChoices = new ArrayList<>();
    for (int i = 0; i < schedule.size(); i++) {
      originalChoices.add(schedule.getChoice(i).getCopy());
    }

    // clear backtracks at all predecessor depths
    for (int i = 0; i < backtrackChoiceDepth; i++) {
      schedule.getChoice(i).clearBacktrack();
    }
    return originalChoices;
  }

  private void setBacktrackTaskAtDepth(
      BacktrackTask parentTask, int backtrackChoiceDepth, boolean isExact) {
    // create a copy of original choices
    List<Schedule.Choice> originalChoices = clearAndReturnOriginalTask(backtrackChoiceDepth);
    if (isExact) {
      // clear the complete choice information (including repeats and backtracks) at all successor
      // depths
      for (int i = backtrackChoiceDepth + 1; i < schedule.size(); i++) {
        schedule.clearChoice(i);
      }
    }

    BigDecimal prefixCoverage =
        PSymGlobal.getCoverage().getPathCoverageAtDepth(backtrackChoiceDepth);

    BacktrackTask newTask = new BacktrackTask(allTasks.size());
    newTask.setPrefixCoverage(prefixCoverage);
    newTask.setDepth(schedule.getChoice(backtrackChoiceDepth).getSchedulerDepth());
    newTask.setChoiceDepth(backtrackChoiceDepth);
    newTask.setChoices(schedule.getChoices());
    newTask.setPerChoiceDepthStats(PSymGlobal.getCoverage().getPerChoiceDepthStats());
    newTask.setParentTask(parentTask);
    newTask.setPriority();
    allTasks.add(newTask);
    parentTask.addChild(newTask);
    addPendingTask(newTask);

    // restore schedule to original choices
    schedule.setChoices(originalChoices);
  }

  /** Set next backtrack task with given orchestration mode */
  public BacktrackTask setNextBacktrackTask() throws InterruptedException {
    if (pendingTasks.isEmpty()) return null;
    BacktrackTask latestTask = BacktrackTask.getNextTask();
    latestTaskId = latestTask.getId();
    assert (!latestTask.isCompleted());
    removePendingTask(latestTask);

    schedule.getChoices().clear();
    PSymGlobal.getCoverage().getPerChoiceDepthStats().clear();
    assert (!latestTask.isInitialTask());
    latestTask.getParentTask().cleanup();

    schedule.setChoices(latestTask.getChoices());
    PSymGlobal.getCoverage().setPerChoiceDepthStats(latestTask.getPerChoiceDepthStats());
    return latestTask;
  }

  private void addPendingTask(BacktrackTask task) {
    pendingTasks.add(task.getId());
    numPendingBacktracks += task.getNumBacktracks();
    numPendingDataBacktracks += task.getNumDataBacktracks();
  }

  private void removePendingTask(BacktrackTask task) {
    pendingTasks.remove(task.getId());
    numPendingBacktracks -= task.getNumBacktracks();
    numPendingDataBacktracks -= task.getNumDataBacktracks();
  }

  /** Reset scheduler state */
  public void reset() {
    depth = 0;
    choiceDepth = 0;
    done = Guard.constFalse();
    stickyStep = true;
    machineCounters.clear();
    //        machines.clear();
    currentMachines.clear();
    PSymGlobal.getSymmetryTracker().reset();
    srcState.clear();
    schedule.setSchedulerDepth(getDepth());
    schedule.setSchedulerChoiceDepth(getChoiceDepth());
    schedule.setSchedulerState(srcState, machineCounters);
    schedule.setSchedulerSymmetry();
  }

  public void reset_stats() {
    searchStats.reset_stats();
    distinctStates.clear();
    totalStateCount = 0;
    totalDistinctStateCount = 0;
    PSymGlobal.getCoverage().resetCoverage();
    if (PSymGlobal.getConfiguration().isChoiceOrchestrationLearning()) {
      PSymGlobal.getChoiceLearningStats()
          .setProgramStateHash(this, PSymGlobal.getConfiguration().getChoiceLearningStateMode(), null);
    }
  }

  /** Reinitialize scheduler */
  public void reinitialize() {
    // set all transient data structures
    srcState = new HashMap<>();
    distinctStates = new HashMap<>();
    isDistinctState = true;
    for (Machine machine : schedule.getMachines()) {
      machine.setScheduler(this);
    }
  }

  /** Restore scheduler state */
  public void restore(int d, int cd) {
    depth = d;
    choiceDepth = cd;
    done = Guard.constFalse();
  }

  public void restoreState(Schedule.ChoiceState state) {
    assert (state != null);
    currentMachines.clear();
    for (Map.Entry<Machine, MachineLocalState> entry : state.getMachineStates().entrySet()) {
      entry.getKey().setMachineLocalState(entry.getValue());
      currentMachines.add(entry.getKey());
    }
    for (Machine m : machines) {
      if (!state.getMachineStates().containsKey(m)) {
        m.reset();
      }
    }
    assert (machines.size() >= currentMachines.size());
    machineCounters = state.getMachineCounters();
  }

  private void storeSrcState() {
    if (!srcState.isEmpty()) return;
    for (Machine machine : currentMachines) {
      MachineLocalState machineLocalState = machine.getMachineLocalState();
      srcState.put(machine, machineLocalState);
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
            + latestTaskId
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

    if (stickyStep || (choiceDepth <= backtrackDepth) || (mode == StateCachingMode.None)) {
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
        if (mode == StateCachingMode.Fast) {
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
      distinctStates.put(concreteState, iter);
      totalDistinctStateCount += 1;
      isDistinctState = true;
      return new int[] {1, 1, -1};
    }
  }
}
