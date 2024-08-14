package pex.runtime.scheduler.replay;

import pex.runtime.PExGlobal;
import pex.runtime.logger.PExLogger;
import pex.runtime.logger.ScheduleWriter;
import pex.runtime.logger.TextWriter;
import pex.runtime.machine.PMachine;
import pex.runtime.machine.PMachineId;
import pex.runtime.scheduler.Schedule;
import pex.runtime.scheduler.Scheduler;
import pex.utils.misc.Assert;
import pex.values.PValue;

import java.util.List;
import java.util.concurrent.TimeoutException;

public class ReplayScheduler extends Scheduler {
    public ReplayScheduler(Schedule sch) {
        super(sch);
    }

    @Override
    public void run() throws TimeoutException, InterruptedException {
        PExLogger.logStartReplay();

        ScheduleWriter.Initialize();
        TextWriter.Initialize();

        ScheduleWriter.logHeader();

        // log run test
        PExLogger.logRunTest();

        stepState.resetToZero();
        start();
        runIteration();
    }

    @Override
    protected void runIteration() throws TimeoutException {
        isDoneStepping = false;
        scheduleTerminated = false;
        while (!isDoneStepping) {
            runStep();
        }

        // check if cycle detected error
        if (Assert.getFailureType().equals("cycle")) {
            Assert.cycle("Cycle detected: Infinite loop found due to revisiting a state multiple times in the same schedule");
        }

        if (Assert.getFailureType().equals("deadlock") && scheduleTerminated) {
            // schedule terminated, check for deadlock
            checkDeadlock();
        }

        if (Assert.getFailureType().equals("liveness")) {
            // check for liveness
            checkLiveness(scheduleTerminated);
        }

        Assert.fromModel(
                !PExGlobal.getConfig().isFailOnMaxStepBound() || (stepNumber < PExGlobal.getConfig().getMaxStepBound()),
                "Step bound of " + PExGlobal.getConfig().getMaxStepBound() + " reached.");
    }

    @Override
    protected void runStep() throws TimeoutException {
        // get a scheduling choice as sender machine
        PMachine sender = getNextScheduleChoice();

        if (sender == null) {
            // done with this schedule
            scheduleTerminated = true;
            isDoneStepping = true;
            PExLogger.logFinishedIteration(stepNumber);
            return;
        }

        // execute a step from message in the sender queue
        executeStep(sender);
    }

    @Override
    protected void reset() {
        super.reset();
    }

    @Override
    protected PMachine getNextScheduleChoice() {
        if (choiceNumber >= schedule.size()) {
            return null;
        }

        // pick the current schedule choice
        PMachineId pid = schedule.getCurrentScheduleChoice(choiceNumber);
        if (pid == null) {
            return null;
        }

        PMachine result = PExGlobal.getGlobalMachine(pid);
        ScheduleWriter.logScheduleChoice(result);
        PExLogger.logRepeatScheduleChoice(result, stepNumber, choiceNumber);

        choiceNumber++;
        return result;
    }

    @Override
    protected PValue<?> getNextDataChoice(List<PValue<?>> input_choices) {
        if (choiceNumber >= schedule.size()) {
            return null;
        }

        // pick the current data choice
        PValue<?> result = schedule.getCurrentDataChoice(choiceNumber);
        assert (input_choices.contains(result));
        ScheduleWriter.logDataChoice(result);
        PExLogger.logRepeatDataChoice(result, stepNumber, choiceNumber);

        choiceNumber++;
        return result;
    }
}
