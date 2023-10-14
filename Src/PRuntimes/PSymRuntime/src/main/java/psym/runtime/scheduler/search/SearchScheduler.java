package psym.runtime.scheduler.search;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.*;
import java.util.concurrent.TimeoutException;
import java.util.function.BiConsumer;
import java.util.function.Consumer;
import java.util.function.Function;
import java.util.function.Supplier;
import java.util.stream.Collectors;
import lombok.Getter;
import lombok.Setter;
import psym.runtime.PSymGlobal;
import psym.runtime.Program;
import psym.runtime.logger.*;
import psym.runtime.machine.Machine;
import psym.runtime.machine.MachineLocalState;
import psym.runtime.scheduler.Schedule;
import psym.runtime.scheduler.Scheduler;
import psym.runtime.scheduler.search.choiceorchestration.*;
import psym.runtime.scheduler.search.symmetry.SymmetryMode;
import psym.runtime.scheduler.search.taskorchestration.BacktrackTask;
import psym.runtime.scheduler.search.taskorchestration.TaskOrchestrationMode;
import psym.runtime.statistics.SearchStats;
import psym.utils.Assert;
import psym.utils.monitor.MemoryMonitor;
import psym.utils.random.NondetUtil;
import psym.valuesummary.*;

/** Represents the search scheduler */
public abstract class SearchScheduler extends Scheduler {
  /** List of all backtrack tasks */
  private final List<BacktrackTask> allTasks = new ArrayList<>();
  /** Priority queue of all backtrack tasks that are pending */
  private final Set<Integer> pendingTasks = new HashSet<>();
  /** List of all backtrack tasks that finished */
  @Getter private final List<Integer> finishedTasks = new ArrayList<>();
  private final ChoiceOrchestrator choiceOrchestrator;
  /** Source state at the beginning of each schedule step */
  protected transient Map<Machine, MachineLocalState> srcState = new HashMap<>();
  @Getter private int iter = 0;
  @Getter private int start_iter = 0;
  @Getter private int backtrackDepth = 0;
  private boolean isDoneIterating = false;
  /** Task id of the latest backtrack task */
  @Getter private int latestTaskId = 0;
  private int numPendingBacktracks = 0;
  private int numPendingDataBacktracks = 0;
  /** Time of last report */
  @Getter @Setter
  private transient Instant lastReportTime = Instant.now();
  protected SearchScheduler(Program p) {
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

  @Override
  public void doSearch() throws TimeoutException, InterruptedException {
    resetBacktrackTasks();
    boolean initialRun = true;
    PSymGlobal.setResult("incomplete");
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
    if (terminalLivenessEnabled) {
      checkLiveness(Guard.constTrue());
    }
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
  public PrimitiveVS<Machine> allocateMachine(
          Guard pc,
          Class<? extends Machine> machineType,
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
        if (PSymGlobal.getConfiguration().getSymmetryMode() != SymmetryMode.None) {
          PSymGlobal.getSymmetryTracker().createMachine(m, g);
        }
      }
    } else {
      Machine newMachine = setupNewMachine(pc, guardedCount, constructor);

      allocated = new PrimitiveVS<>(newMachine).restrict(pc);
      if (PSymGlobal.getConfiguration().getSymmetryMode() != SymmetryMode.None) {
        PSymGlobal.getSymmetryTracker().createMachine(newMachine, pc);
      }
    }

    guardedCount = IntegerVS.add(guardedCount, 1);

    PrimitiveVS<Integer> mergedCount =
            machineCounters.get(machineType).updateUnderGuard(pc, guardedCount);
    machineCounters.put(machineType, mergedCount);
    return allocated;
  }

  protected void storeSrcState() {
    if (!srcState.isEmpty()) return;
    for (Machine machine : currentMachines) {
      MachineLocalState machineLocalState = machine.getMachineLocalState();
      srcState.put(machine, machineLocalState);
    }
  }

  protected void recordResult(SearchStats.TotalStats totalStats) {
    String result = "";
    if (start_iter != 0) {
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
        result += "partially correct";
        if ((iter - start_iter) <= 1) {
          result += " up to step " + safeDepth;
        }
        result +=" with " + getTotalNumBacktracks() + " backtracks remaining";
      }
    }
    PSymGlobal.setResult(result);
  }

  private void summarizeIteration(int startDepth) throws InterruptedException {
    if (PSymGlobal.getConfiguration().getVerbosity() > 3) {
      SearchLogger.logIterationStats(searchStats.getIterationStats().get(iter));
    }
    if (PSymGlobal.getConfiguration().getMaxExecutions() > 0) {
      isDoneIterating = ((iter - start_iter) >= PSymGlobal.getConfiguration().getMaxExecutions());
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

  /** Reset scheduler state */
  protected void reset() {
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
    terminalLivenessEnabled = true;
  }

  /** Restore scheduler state */
  protected void restore(int d, int cd) {
    depth = d;
    choiceDepth = cd;
    done = Guard.constFalse();
    terminalLivenessEnabled = true;
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

  private void resetBacktrackTasks() {
    pendingTasks.clear();
    numPendingBacktracks = 0;
    numPendingDataBacktracks = 0;
    BacktrackTask.initialize(PSymGlobal.getConfiguration().getTaskOrchestration());
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
          break;
        }
        if (PSymGlobal.getConfiguration().getMaxPendingBacktrackTasks() > 0
                && pendingTasks.size() >= PSymGlobal.getConfiguration().getMaxPendingBacktrackTasks()) {
          setBacktrackTaskAtDepthCombined(parentTask, i);
          break;
        }
        // top backtrack should be never combined
        setBacktrackTaskAtDepthExact(parentTask, i);
        numBacktracksAdded++;
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

  private BacktrackTask getTask(int taskId) {
    isValidTaskId(taskId);
    return allTasks.get(taskId);
  }

  private void isValidTaskId(int taskId) {
    assert (taskId < allTasks.size());
  }

  private void setBacktrackTaskAtDepthExact(BacktrackTask parentTask, int backtrackChoiceDepth) {
    setBacktrackTaskAtDepth(parentTask, backtrackChoiceDepth, true);
  }

  private void setBacktrackTaskAtDepthCombined(BacktrackTask parentTask, int backtrackChoiceDepth) {
    setBacktrackTaskAtDepth(parentTask, backtrackChoiceDepth, false);
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

  protected List<Schedule.Choice> clearAndReturnOriginalTask(int backtrackChoiceDepth) {
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

  protected int getTotalNumBacktracks() {
    int count = schedule.getNumBacktracksInSchedule();
    count += numPendingBacktracks;
    return count;
  }

  protected double getTotalDataBacktracksPercent() {
    int totalBacktracks = getTotalNumBacktracks();
    if (totalBacktracks == 0) {
      return 0.0;
    }
    int count = schedule.getNumDataBacktracksInSchedule();
    count += numPendingDataBacktracks;
    return (count * 100.0) / totalBacktracks;
  }

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
    int bound = isData ? PSymGlobal.getConfiguration().getDataChoiceBound() : PSymGlobal.getConfiguration().getSchChoiceBound();

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
      SearchLogger.logMessage("new choice at depth " + depth);
      choices = getChoices.get();
      if (PSymGlobal.getConfiguration().getSymmetryMode() != SymmetryMode.None) {
        choices = PSymGlobal.getSymmetryTracker().getReducedChoices(choices, isData);
      }
      choices =
              choices.stream()
                      .map(x -> x.restrict(schedule.getFilter()))
                      .filter(x -> !(x.getUniverse().isFalse()))
                      .collect(Collectors.toList());
      isNewChoice = true;
    }

    if (choices.size() > 1) {
      choiceOrchestrator.reorderChoices(choices, bound, isData);
    }

    List<ValueSummary> chosen = new ArrayList();
    ChoiceQTable.ChoiceQStateKey chosenQStateKey = new ChoiceQTable.ChoiceQStateKey();
    List<ValueSummary> backtrack = new ArrayList();
    for (int i = 0; i < choices.size(); i++) {
      ValueSummary choice = choices.get(i);
      if ((bound <= 0) || (i < bound)) {
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

  protected abstract void printCurrentStatus(double newRuntime);
  protected abstract void printProgressHeader(boolean consolePrint);
  protected abstract void printProgress(boolean forcePrint);
  public abstract void print_search_stats();
  protected abstract void reset_stats();
  /**
   * Estimates and prints a coverage percentage based on number of choices explored versus remaining
   * at each depth
   */
  public abstract void reportEstimatedCoverage();


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

  @Override
  public ValueSummary getNextPrimitiveList(ListVS<? extends ValueSummary> candidates, Guard pc) {
    int depth = choiceDepth;
    PrimitiveVS<ValueSummary> res =
            getNext(
                    depth,
                    schedule::getRepeatElement,
                    schedule::getBacktrackElement,
                    schedule::clearBacktrack,
                    schedule::addRepeatElement,
                    schedule::addBacktrackElement,
                    () -> super.getNextElementChoices(candidates, pc),
                    super::getNextElementHelper,
                    true);
    choiceDepth = depth + 1;
    return super.getNextElementFlattener(res);
  }

  public void print_stats(SearchStats.TotalStats totalStats, double timeUsed, double memoryUsed) {
    printProgress(true);
    if (!isFinalResult) {
      recordResult(totalStats);
      isFinalResult = true;
    }

    SearchLogger.log("\n--------------------");

    // print basic statistics
    StatWriter.log("time-seconds", String.format("%.1f", timeUsed));
    StatWriter.log("memory-max-MB", String.format("%.1f", MemoryMonitor.getMaxMemSpent()));
    StatWriter.log("memory-current-MB", String.format("%.1f", memoryUsed));
    StatWriter.log(
        "max-depth-explored", String.format("%d", totalStats.getDepthStats().getDepth()));
    SearchLogger.log(
        String.format("Max Depth Explored       %d", totalStats.getDepthStats().getDepth()));

    // print learn statistics
    StatWriter.log(
            "learn-#-qstates", String.format("%d", PSymGlobal.getChoiceLearningStats().numQStates()));
    StatWriter.log(
            "learn-#-qvalues", String.format("%d", PSymGlobal.getChoiceLearningStats().numQValues()));

    // print task statistics
    StatWriter.log("#-tasks-finished", String.format("%d", getFinishedTasks().size()));
    StatWriter.log(
            "#-tasks-remaining", String.format("%d", (allTasks.size() - getFinishedTasks().size())));
    StatWriter.log("#-backtracks", String.format("%d", getTotalNumBacktracks()));
    StatWriter.log("%-backtracks-data", String.format("%.2f", getTotalDataBacktracksPercent()));
    StatWriter.log("#-schedules", String.format("%d", (getIter() - getStart_iter())));

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
        Guard initCond = machine.getEventBuffer().hasCreateMachineUnderGuard().getGuardFor(true).and(schedule.getFilter());
        if (!initCond.isFalse()) {
          PrimitiveVS<Machine> ret = new PrimitiveVS<>(machine).restrict(initCond);
          return new ArrayList<>(Collections.singletonList(ret));
        }
      }
    }

    // prioritize the sync actions i.e. events that are marked as synchronous
    for (Machine machine : machines) {
      if (!machine.getEventBuffer().isEmpty()) {
        Guard syncCond = machine.getEventBuffer().hasSyncEventUnderGuard().getGuardFor(true).and(schedule.getFilter());
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
            machine.getEventBuffer().satisfiesPredUnderGuard(x -> x.canRun()).getGuardFor(true).and(schedule.getFilter());
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
