package psym.runtime.scheduler.replay;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.TimeoutException;
import java.util.function.Function;
import lombok.Getter;
import org.apache.commons.lang3.NotImplementedException;
import psym.runtime.PSymGlobal;
import psym.runtime.Program;
import psym.runtime.logger.PSymLogger;
import psym.runtime.logger.ScheduleWriter;
import psym.runtime.logger.SearchLogger;
import psym.runtime.logger.TraceLogger;
import psym.runtime.machine.Machine;
import psym.runtime.machine.events.Message;
import psym.runtime.scheduler.Schedule;
import psym.runtime.scheduler.Scheduler;
import psym.runtime.scheduler.symmetry.SymmetryMode;
import psym.utils.Assert;
import psym.valuesummary.*;

public class ReplayScheduler extends Scheduler {
  @Getter
  /** Path constraint */
  private final Guard pathConstraint;
  /** Counterexample length */
  private final int cexLength;

  public ReplayScheduler(
      Program p,
      Schedule schedule,
      Guard pc,
      int length) {
    super(p);
    TraceLogger.enable();
    this.schedule = schedule.guard(pc).getSingleSchedule();
    for (Machine machine : schedule.getMachines()) {
      machine.reset();
    }
    PSymGlobal.getConfiguration().setToReplay();
    cexLength = length;
    pathConstraint = pc;
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
      PSymLogger.info(".. Reading schedule from file " + readFromFile);
      throw new NotImplementedException("Replaying a schedule is currently unsupported.");
//      PSymLogger.info(".. Successfully read.");
    } catch (NotImplementedException e) {
      e.printStackTrace();
      throw new RuntimeException(".. Failed to read schedule from file " + readFromFile, e);
    }
//    return result;
  }

  @Override
  public void doSearch() throws TimeoutException {
    TraceLogger.logStartReplayCex(cexLength);
    ScheduleWriter.logHeader();
    initializeSearch();
    performSearch();
  }

  @Override
  public void resumeSearch() throws InterruptedException {
    throw new InterruptedException("Not implemented");
  }

  @Override
  protected void performSearch() throws TimeoutException {
    while (!isDone()) {
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
  public void step() {
    allMachinesHalted = Guard.constFalse();

    removeHalted();

    PrimitiveVS<Machine> schedulingChoices = getNextSchedulingChoice();

    if (schedulingChoices.isEmptyVS()) {
      done = Guard.constTrue();
      SearchLogger.finishedExecution(depth);
    }

    if (done.isTrue()) {
      return;
    }

    Message effect = null;
    List<Message> effects = new ArrayList<>();

    for (GuardedValue<Machine> schedulingChoice : schedulingChoices.getGuardedValues()) {
      Machine machine = schedulingChoice.getValue();
      Guard guard = schedulingChoice.getGuard();
      Message removed = rmBuffer(machine, guard);
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

    TraceLogger.schedule(depth, effect);

    performEffect(effect);
  }

  @Override
  public PrimitiveVS<Machine> getNextSchedulingChoice() {
    PrimitiveVS<Machine> res = schedule.getRepeatSchedulingChoice(choiceDepth);
    if (res.isEmptyVS()) {
      allMachinesHalted = Guard.constTrue();
    }
    choiceDepth++;
    return res;
  }

  @Override
  public PrimitiveVS<Boolean> getNextBoolean(Guard pc) {
    PrimitiveVS<Boolean> res = schedule.getRepeatBool(choiceDepth);
    ScheduleWriter.logBoolean(res);
    choiceDepth++;
    return res;
  }

  @Override
  public PrimitiveVS<Integer> getNextInteger(PrimitiveVS<Integer> bound, Guard pc) {
    PrimitiveVS<Integer> res = schedule.getRepeatInt(choiceDepth);
    ScheduleWriter.logInteger(res);
    choiceDepth++;
    return res;
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

    PrimitiveVS<Machine> allocated = schedule.getMachine(machineType, guardedCount);
    TraceLogger.onCreateMachine(pc, allocated.getValues().iterator().next());
    allocated.getValues().iterator().next().setScheduler(this);

    guardedCount = IntegerVS.add(guardedCount, 1);

    PrimitiveVS<Integer> mergedCount =
            machineCounters.get(machineType).updateUnderGuard(pc, guardedCount);
    machineCounters.put(machineType, mergedCount);
    return allocated;
  }

  @Override
  public boolean isDone() {
    return super.isDone() || this.getChoiceDepth() >= schedule.size();
  }

}
