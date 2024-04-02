package pexplicit.runtime.scheduler.explicit;

import java.time.Duration;
import java.time.Instant;
import java.util.*;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;

import lombok.Getter;
import lombok.Setter;
import org.apache.commons.lang3.StringUtils;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.logger.ScratchLogger;
import pexplicit.runtime.logger.StatWriter;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.events.PMessage;
import pexplicit.runtime.scheduler.Choice;
import pexplicit.runtime.scheduler.Scheduler;
import pexplicit.utils.misc.Assert;
import pexplicit.utils.monitor.MemoryMonitor;
import pexplicit.utils.monitor.TimeMonitor;
import pexplicit.values.PValue;

/**
 * Represents the scheduler for performing explicit-state model checking
 */
public class ExplicitSearchScheduler extends Scheduler {
  /**
   * Current iteration
   */
  @Getter
  private int iteration = 0;

  /**
   * Max step number explored
   */
  @Getter
  private int maxStepNumber = 0;

  /**
   * Backtrack choice number
   */
  @Getter
  private int backtrackChoiceNumber = 0;

  /**
   * Whether done with all iterations
   */
  private boolean isDoneIterating = false;

  /**
   * Whether done with current iteration
   */
  private boolean isDoneStepping = false;

  /**
   * Time of last status report
   */
  @Getter
  @Setter
  private transient Instant lastReportTime = Instant.now();

  /**
   * Constructor.
   */
  public ExplicitSearchScheduler() {
    super();
  }

  /**
   * Run the scheduler to perform explicit-state search.
   *
   * @throws TimeoutException Throws timeout exception if timeout is reached
   */
  @Override
  public void run() throws TimeoutException {
    PExplicitGlobal.setResult("incomplete");
    start();

    if (PExplicitGlobal.getConfig().getVerbosity() == 0) {
      printProgressHeader(true);
    }
    while (!isDoneIterating) {
      iteration++;
      PExplicitLogger.logStartIteration(iteration, schedule.getStepNumber());
      runIteration();
      postProcessIteration();
    }
  }

  /**
   * Run an iteration.
   * @throws TimeoutException Throws timeout exception if timeout is reached.
   */
  @Override
  protected void runIteration() throws TimeoutException {
    while (!isDoneStepping) {
      printProgress(false);
      runStep();
    }
    printProgress(false);
    if (schedule.getStepNumber() > maxStepNumber) {
      maxStepNumber = schedule.getStepNumber();
    }
    Assert.prop(
            !PExplicitGlobal.getConfig().isFailOnMaxStepBound() || (schedule.getStepNumber() < PExplicitGlobal.getConfig().getMaxStepBound()),
            "Step bound of " + PExplicitGlobal.getConfig().getMaxStepBound() + " reached.");
    if (PExplicitGlobal.getConfig().getMaxSchedules() > 0) {
      isDoneIterating = (iteration >= PExplicitGlobal.getConfig().getMaxSchedules());
    }
  }

  /**
   * Run a step in the current iteration.
   * @throws TimeoutException Throws timeout exception if timeout is reached.
   */
  @Override
  protected void runStep() throws TimeoutException {
    // check for timeout/memout
    TimeMonitor.checkTimeout();
    MemoryMonitor.checkMemout();

    // get a scheduling choice as sender machine
    PMachine sender = getNextScheduleChoice();

    if (sender == null) {
      // no scheduling choice remains, done with this schedule
      isDoneStepping = true;
      PExplicitLogger.logFinishedIteration(schedule.getStepNumber());
      return;
    }

    // pop message from sender queue
    PMessage msg = sender.getSendBuffer().remove();

    if (!msg.getEvent().isCreateMachineEvent()) {
      // update step number
      schedule.setStepNumber(schedule.getStepNumber()+1);
    }

    // log start step
    PExplicitLogger.logStartStep(schedule.getStepNumber(), sender, msg);

    // process message
    msg.getTarget().processEventToCompletion(msg);
  }

  /**
   * Reset the scheduler.
   */
  @Override
  protected void reset() {
    schedule.setStepNumber(0);
    schedule.setChoiceNumber(0);
    schedule.getMachineListByType().clear();
    schedule.getMachineSet().clear();
    terminalLivenessEnabled = true;
  }

  /**
   * Get the next schedule choice.
   * @return Machine as scheduling choice.
   */
  @Override
  public PMachine getNextScheduleChoice() {
    PMachine result;

    if (schedule.getChoiceNumber() < backtrackChoiceNumber) {
      // pick the current schedule choice
      result = schedule.getCurrentScheduleChoice(schedule.getChoiceNumber());
      schedule.setChoiceNumber(schedule.getChoiceNumber()+1);
      return result;
    }

    // get existing unexplored choices, if any
    List<PMachine> choices = schedule.getUnexploredScheduleChoices(schedule.getChoiceNumber());

    if (choices.isEmpty()) {
      // no existing unexplored choices, so try generating new choices
      choices = getNewScheduleChoices();
      if (choices.isEmpty()) {
        // no unexplored choices remaining
        schedule.setChoiceNumber(schedule.getChoiceNumber()+1);
        return null;
      }
    }

    // pick the first choice
    result = choices.get(0);

    // remove the first choice from unexplored choices
    choices.remove(0);

    // set unexplored choices
    schedule.setUnexploredScheduleChoices(choices, schedule.getChoiceNumber());

    // increment choice number
    schedule.setChoiceNumber(schedule.getChoiceNumber()+1);

    return result;
  }

  /**
   * Get the next data choice.
   * @return PValue as data choice
   */
  @Override
  public PValue<?> getNextDataChoice(List<PValue<?>> input_choices) {
    PValue<?> result;

    if (schedule.getChoiceNumber() < backtrackChoiceNumber) {
      // pick the current data choice
      result = schedule.getCurrentDataChoice(schedule.getChoiceNumber());
      assert (input_choices.contains(result));
      schedule.setChoiceNumber(schedule.getChoiceNumber()+1);
      return result;
    }

    // get existing unexplored choices, if any
    List<PValue<?>> choices = schedule.getUnexploredDataChoices(schedule.getChoiceNumber());
    assert (input_choices.containsAll(choices));

    if (choices.isEmpty()) {
      // no existing unexplored choices, so try generating new choices
      choices = input_choices;
      if (choices.isEmpty()) {
        // no unexplored choices remaining
        schedule.setChoiceNumber(schedule.getChoiceNumber()+1);
        return null;
      }
    }

    // pick the first choice
    result = choices.get(0);

    // remove the first choice from unexplored choices
    choices.remove(0);

    // set unexplored choices
    schedule.setUnexploredDataChoices(choices, schedule.getChoiceNumber());

    // increment choice number
    schedule.setChoiceNumber(schedule.getChoiceNumber()+1);

    return result;
  }

  private void postProcessIteration() {
    if (!isDoneIterating) {
      postIterationCleanup();
    }
  }

  private void postIterationCleanup() {
    for (int cIdx = schedule.size() - 1; cIdx >= 0; cIdx--) {
      Choice choice = schedule.getChoice(cIdx);
      schedule.clearCurrent(cIdx);
      if (choice.isUnexploredNonEmpty()) {
        PExplicitLogger.logBacktrack(cIdx);
        backtrackChoiceNumber = cIdx;
        for (PMachine machine : schedule.getMachineSet()) {
          machine.reset();
        }
        reset();
        start();
        return;
      } else {
        schedule.clearChoice(cIdx);
      }
    }
    isDoneIterating = true;
  }

  private List<PMachine> getNewScheduleChoices() {
    // prioritize create machine events
    for (PMachine machine : schedule.getMachineSet()) {
      if (machine.getSendBuffer().nextIsCreateMachineMsg()) {
        return new ArrayList<>(Collections.singletonList(machine));
      }
    }

    // now there are no create machine events remaining
    List<PMachine> choices = new ArrayList<>();

    for (PMachine machine : schedule.getMachineSet()) {
      if (machine.getSendBuffer().nextHasTargetRunning()) {
        choices.add(machine);
      }
    }

    return choices;
  }

  public void updateResult() {
    String result = "";
    int maxStepBound = PExplicitGlobal.getConfig().getMaxStepBound();
    int numUnexplored = schedule.getNumUnexploredChoices();
    if (maxStepNumber < maxStepBound) {
      if (numUnexplored == 0) {
        result += "correct for any depth";
      } else {
        result += String.format("partially correct with %d choices remaining", numUnexplored);
      }
    } else {
      if (numUnexplored == 0) {
        result += String.format("correct up to step %d", maxStepNumber);
      } else {
        result += String.format("partially correct up to step %d with %d choices remaining", maxStepNumber, numUnexplored);
      }
    }
    PExplicitGlobal.setResult(result);
  }

  public void recordStats() {
    double timeUsed = (Duration.between(TimeMonitor.getStart(), Instant.now()).toMillis() / 1000.0);
    double memoryUsed = MemoryMonitor.getMemSpent();

    printProgress(true);
    PExplicitLogger.log("\n--------------------");

    // print basic statistics
    StatWriter.log("time-seconds", String.format("%.1f", timeUsed));
    StatWriter.log("memory-max-MB", String.format("%.1f", MemoryMonitor.getMaxMemSpent()));
    StatWriter.log("memory-current-MB", String.format("%.1f", memoryUsed));
    StatWriter.log("#-schedules", String.format("%d", iteration));
    StatWriter.log("max-step", String.format("%d", maxStepNumber));
    PExplicitLogger.log( String.format("Max Schedule Length       %d", maxStepNumber));
    StatWriter.log("#-choices-unexplored", String.format("%d", schedule.getNumUnexploredChoices()));
    StatWriter.log("%%-choices-unexplored-data", String.format("%.1f", schedule.getUnexploredDataChoicesPercent()));
  }

  private void printCurrentStatus(double newRuntime) {
    StringBuilder s = new StringBuilder(100);

    s.append("--------------------");
    s.append(String.format("\n    Status after %.2f seconds:", newRuntime));
    s.append(String.format("\n      Memory:           %.2f MB", MemoryMonitor.getMemSpent()));
    s.append(String.format("\n      Depth:            %d", schedule.getStepNumber()));

    s.append(String.format("\n      Schedules:        %d", iteration));
    s.append(String.format("\n      Unexplored:       %d", schedule.getNumUnexploredChoices()));

    ScratchLogger.log(s.toString());
  }

  private void printProgressHeader(boolean consolePrint) {
    StringBuilder s = new StringBuilder(100);
    s.append(StringUtils.center("Time", 11));
    s.append(StringUtils.center("Memory", 9));
    s.append(StringUtils.center("Step", 7));

    s.append(StringUtils.center("Schedule", 12));
    s.append(StringUtils.center("Unexplored", 24));

    if (consolePrint) {
      System.out.println(s);
    } else {
      PExplicitLogger.info(s.toString());
    }
  }

  protected void printProgress(boolean forcePrint) {
    if (forcePrint || (TimeMonitor.findInterval(getLastReportTime()) > 5)) {
      setLastReportTime(Instant.now());
      double newRuntime = TimeMonitor.getRuntime();
      printCurrentStatus(newRuntime);
      boolean consolePrint = (PExplicitGlobal.getConfig().getVerbosity() == 0);
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
          PExplicitLogger.info("--------------------");
          printProgressHeader(false);
        }
        s.append(StringUtils.center(String.format("%s", runtimeHms), 11));
        s.append(
            StringUtils.center(String.format("%.1f GB", MemoryMonitor.getMemSpent() / 1024), 9));
        s.append(StringUtils.center(String.format("%d", schedule.getStepNumber()), 7));

        s.append(StringUtils.center(String.format("%d", iteration), 12));
        s.append(
            StringUtils.center(
                String.format(
                    "%d (%.0f %% data)", schedule.getNumUnexploredChoices(), schedule.getUnexploredDataChoicesPercent()),
                24));

        if (consolePrint) {
          System.out.print(s);
        } else {
          PExplicitLogger.log(s.toString());
        }
      }
    }
  }
}
