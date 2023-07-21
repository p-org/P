package psym.runtime.scheduler;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.function.BiConsumer;
import java.util.function.Consumer;
import java.util.function.Function;
import java.util.function.Supplier;
import java.util.stream.Collectors;

import lombok.Getter;
import psym.runtime.PSymGlobal;
import psym.runtime.Program;
import psym.runtime.logger.*;
import psym.runtime.machine.Machine;
import psym.runtime.scheduler.explicit.choiceorchestration.*;
import psym.runtime.scheduler.symmetry.SymmetryMode;
import psym.runtime.statistics.SearchStats;
import psym.utils.monitor.MemoryMonitor;
import psym.utils.random.NondetUtil;
import psym.valuesummary.*;

/** Represents the search scheduler */
public abstract class SearchScheduler extends Scheduler {
  @Getter
  private final ChoiceOrchestrator choiceOrchestrator;
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
      getChoiceOrchestrator().reorderChoices(choices, bound, isData);
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
