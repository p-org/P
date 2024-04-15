package pexplicit.runtime.scheduler;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.PMonitor;
import pexplicit.runtime.machine.events.PMessage;
import pexplicit.utils.exceptions.DeadlockException;
import pexplicit.utils.exceptions.LivenessException;
import pexplicit.utils.exceptions.NotImplementedException;
import pexplicit.values.*;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.TimeoutException;
import java.util.function.Function;

/**
 * Represents the base class that all schedulers extend.
 */
public abstract class Scheduler implements SchedulerInterface {
    /**
     * Current schedule
     */
    public final Schedule schedule;

    @Getter
    @Setter
    protected int stepNumLogs = 0;

    /**
     * Constructor
     */
    protected Scheduler() {
        this.schedule = new Schedule();
    }

    /**
     * Run the scheduler.
     *
     * @throws TimeoutException     Throws timeout exception if timeout is reached
     * @throws InterruptedException Throws interrupt exception if interrupted
     */
    public abstract void run() throws TimeoutException, InterruptedException;

    /**
     * Run an iteration.
     *
     * @throws TimeoutException Throws timeout exception if timeout is reached.
     */
    protected abstract void runIteration() throws TimeoutException;

    /**
     * Run a step in the current iteration.
     *
     * @throws TimeoutException Throws timeout exception if timeout is reached.
     */
    protected abstract void runStep() throws TimeoutException;

    /**
     * Reset the scheduler.
     */
    protected abstract void reset();

    /**
     * Get the next schedule choice.
     *
     * @return PMachine as schedule choice
     */
    protected abstract PMachine getNextScheduleChoice();

    /**
     * Get the next data choice.
     *
     * @return PValue as data choice
     */
    protected abstract PValue<?> getNextDataChoice(List<PValue<?>> input_choices);

    public void updateLogNumber() {
        stepNumLogs += 1;
        if (stepNumLogs >= PExplicitGlobal.getConfig().getMaxStepLogBound()) {
            throw new LivenessException("Detected potential infinite loop in an atomic block");
        }
    }

    /**
     * Get the next random boolean choice
     *
     * @return boolean data choice
     */
    public PBool getRandomBool() {
        List<PValue<?>> choices = new ArrayList<>();
        choices.add(PBool.PTRUE);
        choices.add(PBool.PFALSE);
        return (PBool) getNextDataChoice(choices);
    }

    /**
     * Get the next random integer choice
     *
     * @param bound upper bound (exclusive) on the integer.
     * @return integer data choice
     */
    public PInt getRandomInt(PInt bound) {
        List<PValue<?>> choices = new ArrayList<>();
        int boundInt = bound.getValue();
        if (boundInt == 0) {
            boundInt = 1;
        }
        for (int i = 0; i < boundInt; i++) {
            choices.add(new PInt(i));
        }
        return (PInt) getNextDataChoice(choices);
    }

    /**
     * Get the next random element from a collection.
     *
     * @param choices List of data choices
     * @return data choice
     */
    protected PValue<?> getRandomEntry(List<PValue<?>> choices) {
        return getNextDataChoice(choices);
    }

    /**
     * Get the next random element from a PSeq.
     *
     * @param seq PSeq object
     * @return data choice
     */
    public PValue<?> getRandomEntry(PSeq seq) {
        return getRandomEntry(seq.toList());
    }

    /**
     * Get the next random element from a PSet.
     *
     * @param set PSet object
     * @return data choice
     */
    public PValue<?> getRandomEntry(PSet set) {
        return getRandomEntry(set.toList());
    }

    /**
     * Get the next random key from a PMap.
     *
     * @param map PMap object
     * @return data choice
     */
    public PValue<?> getRandomEntry(PMap map) {
        return getRandomEntry(map.toList());
    }

    /**
     * Start the scheduler.
     * Starts monitors and main machine.
     */
    protected void start() {
        assert (schedule.getStepNumber() == 0);

        // start monitors first
        for (PMonitor monitor : PExplicitGlobal.getModel().getMonitors()) {
            startMachine(monitor);
        }

        // start main machine
        startMachine(PExplicitGlobal.getModel().getStart());
    }

    /**
     * Start a machine.
     * Runs the constructor of this machine.
     */
    public void startMachine(PMachine machine) {
        if (!PExplicitGlobal.getMachineSet().contains(machine)) {
            // add machine to global context
            PExplicitGlobal.addGlobalMachine(machine, 0);
        }

        // add machine to schedule
        schedule.makeMachine(machine);

        // run create machine event
        processCreateEvent(new PMessage(PEvent.createMachine, machine, null));
    }

    /**
     * Allocate a machine
     *
     * @param machineType
     * @param constructor
     * @return
     */
    public PMachine allocateMachine(
            Class<? extends PMachine> machineType,
            Function<Integer, ? extends PMachine> constructor) {
        // get machine count for given type from schedule
        int machineCount = schedule.getMachineCount(machineType);

        PMachine machine = PExplicitGlobal.getGlobalMachine(machineType, machineCount);
        if (machine == null) {
            // create a new machine
            machine = constructor.apply(machineCount);
            PExplicitGlobal.addGlobalMachine(machine, machineCount);
        }

        // add machine to schedule
        schedule.makeMachine(machine);
        return machine;
    }

    /**
     * Run all monitors observing this message
     *
     * @param message Message
     */
    public void runMonitors(PMessage message) {
        List<PMonitor> listenersForEvent = PExplicitGlobal.getModel().getListeners().get(message.getEvent());
        if (listenersForEvent != null) {
            for (PMonitor m : listenersForEvent) {
                // log monitor process event
                PExplicitLogger.logMonitorProcessEvent(m, message);

                m.processEventToCompletion(message.setTarget(m));
            }
        }
    }

    /**
     * Process the create machine event at the target machine
     *
     * @param message Message to process
     */
    public void processCreateEvent(PMessage message) {
        message.getTarget().processEventToCompletion(message);
    }

    /**
     * Process the dequeue event at the target machine
     *
     * @param message Message to process
     */
    public void processDequeueEvent(PMessage message) {
        // log monitor process event
        PExplicitLogger.logDequeueEvent(message.getTarget(), message);

        message.getTarget().processEventToCompletion(message);
    }

    /**
     * Announce an event to all observing monitors
     *
     * @param event   Event to announce
     * @param payload Event payload
     */
    public void announce(PEvent event, PValue<?> payload) {
        if (event == null) {
            throw new NotImplementedException("Machine cannot announce a null event");
        }
        PMessage message = new PMessage(event, null, payload);
        runMonitors(message);
    }

    /**
     * Check for deadlock at the end of a completed schedule
     */
    public void checkDeadlock() {
        for (PMachine machine: schedule.getMachineSet()) {
            if (machine.canRun() && machine.isBlocked()) {
                throw new DeadlockException(String.format("Deadlock detected. %s is waiting to receive an event, but no other controlled tasks are enabled.", machine));
            }
        }
    }

    /**
     * Check for liveness at the end of a completed schedule
     */
    public void checkLiveness(boolean terminated) {
        for (PMachine monitor: PExplicitGlobal.getModel().getMonitors()) {
            if (monitor.getCurrentState().isHotState()) {
                if (terminated) {
                    throw new LivenessException(String.format("Monitor %s detected liveness bug in hot state %s at the end of program execution", monitor, monitor.getCurrentState()));
                } else {
                    throw new LivenessException(String.format("Monitor %s detected potential liveness bug in hot state %s", monitor, monitor.getCurrentState()));
                }
            }
        }
    }
}
