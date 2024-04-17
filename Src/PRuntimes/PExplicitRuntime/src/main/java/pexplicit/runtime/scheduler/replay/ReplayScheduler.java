package pexplicit.runtime.scheduler.replay;

import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.logger.ScheduleWriter;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.events.PMessage;
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

        // log run test
        PExplicitLogger.logRunTest();

        for (PMachine machine : schedule.getMachineSet()) {
            machine.reset();
        }
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

        if (scheduleTerminated) {
            // schedule terminated, check for deadlock
            checkDeadlock();
        }
        // check for liveness
        checkLiveness(scheduleTerminated);

        Assert.fromModel(
                !PExplicitGlobal.getConfig().isFailOnMaxStepBound() || (schedule.getStepNumber() < PExplicitGlobal.getConfig().getMaxStepBound()),
                "Step bound of " + PExplicitGlobal.getConfig().getMaxStepBound() + " reached.");
    }

    @Override
    protected void runStep() throws TimeoutException {
        // reset number of logs in current step
        stepNumLogs = 0;

        // get a scheduling choice as sender machine
        PMachine sender = getNextScheduleChoice();

        if (sender == null) {
            // done with this schedule
            scheduleTerminated = true;
            isDoneStepping = true;
            PExplicitLogger.logFinishedIteration(schedule.getStepNumber());
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
        if (schedule.getChoiceNumber() >= schedule.size()) {
            return null;
        }

        // pick the current schedule choice
        PMachine result = schedule.getCurrentScheduleChoice(schedule.getChoiceNumber());
        ScheduleWriter.logScheduleChoice(result);
        PExplicitLogger.logRepeatScheduleChoice(result, schedule.getStepNumber(), schedule.getChoiceNumber());

        schedule.setChoiceNumber(schedule.getChoiceNumber() + 1);
        return result;
    }

    @Override
    protected PValue<?> getNextDataChoice(List<PValue<?>> input_choices) {
        if (schedule.getChoiceNumber() >= schedule.size()) {
            return null;
        }

        // pick the current data choice
        PValue<?> result = schedule.getCurrentDataChoice(schedule.getChoiceNumber());
        assert (input_choices.contains(result));
        ScheduleWriter.logDataChoice(result);
        PExplicitLogger.logRepeatDataChoice(result, schedule.getStepNumber(), schedule.getChoiceNumber());

        schedule.setChoiceNumber(schedule.getChoiceNumber() + 1);
        return result;
    }
}
