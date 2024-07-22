package pexplicit.runtime.scheduler.replay;

import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.logger.ScheduleWriter;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.PMachineId;
import pexplicit.runtime.scheduler.Schedule;
import pexplicit.runtime.scheduler.Scheduler;
import pexplicit.utils.misc.Assert;
import pexplicit.values.PValue;

import java.util.List;
import java.util.concurrent.TimeoutException;

public class ReplayScheduler extends Scheduler {
    public ReplayScheduler(Schedule sch) {
        super(sch);
    }

    @Override
    public void run() throws TimeoutException, InterruptedException {
        PExplicitLogger.logStartReplay();
        ScheduleWriter.logHeader();

        // log run test // Add PexplicitGobal.tIDtoLocalTID here
        PExplicitLogger.logRunTest();

        // stepState.resetToZero();
        start();
        runIteration();
    }

    @Override
    public void runParallel(int tID) throws TimeoutException, InterruptedException {
        run();
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
                !PExplicitGlobal.getConfig().isFailOnMaxStepBound() || (stepNumber < PExplicitGlobal.getConfig().getMaxStepBound()),
                "Step bound of " + PExplicitGlobal.getConfig().getMaxStepBound() + " reached.");
    }

    @Override
    protected void runStep() throws TimeoutException {
        // get a scheduling choice as sender machine
        PMachine sender = getNextScheduleChoice();

        if (sender == null) {
            // done with this schedule
            scheduleTerminated = true;
            isDoneStepping = true;
            PExplicitLogger.logFinishedIteration(stepNumber);
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

        PMachine result = PExplicitGlobal.getScheduler().getGlobalMachine(pid);
        ScheduleWriter.logScheduleChoice(result);
        PExplicitLogger.logRepeatScheduleChoice(result, stepNumber, choiceNumber);

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
        PExplicitLogger.logRepeatDataChoice(result, stepNumber, choiceNumber);

        choiceNumber++;
        return result;
    }
}
