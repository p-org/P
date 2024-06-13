package pexplicit.runtime.scheduler;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.PMachineId;
import pexplicit.runtime.machine.PMonitor;
import pexplicit.runtime.scheduler.explicit.StepState;
import pexplicit.utils.exceptions.BugFoundException;
import pexplicit.utils.exceptions.NotImplementedException;
import pexplicit.utils.misc.Assert;
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
    /**
     * Step number
     */
    @Getter
    @Setter
    protected int stepNumber = 0;
    /**
     * Choice number
     */
    @Getter
    @Setter
    protected int choiceNumber = 0;
    /**
     * Current step state
     */
    @Getter
    protected StepState stepState = new StepState();
    /**
     * Whether done with current iteration
     */
    protected transient boolean isDoneStepping = false;

    /**
     * Whether schedule terminated
     */
    protected transient boolean scheduleTerminated = false;

    @Getter
    @Setter
    protected transient int stepNumLogs = 0;
    /**
     * Whether last step was a sticky step (i.e., createMachine step)
     */
    protected boolean isStickyStep = true;

    /**
     * Constructor
     */
    protected Scheduler(Schedule sch) {
        this.schedule = sch;
    }

    /**
     * Constructor
     */
    protected Scheduler() {
        this(new Schedule());
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
    protected void reset() {
        stepNumber = 0;
        choiceNumber = 0;
        isStickyStep = true;
    }

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
            Assert.liveness("Detected potential infinite loop in an atomic block");
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
        if (boundInt > 10000) {
            throw new BugFoundException(String.format("choose expects a parameter with at most 10,000 choices, got %d choices instead.", boundInt));
        }
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
        PInt randomEntryIdx = getRandomInt(new PInt(choices.size()));
        return choices.get(randomEntryIdx.getValue());
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
        stepState.makeMachine(machine);

        // run create machine event
        processCreateEvent(new PMessage(PEvent.createMachine, machine, null));
    }

    /**
     * Execute a step by popping a message from the sender FIFO buffer and handling it
     *
     * @param sender Sender machine
     */
    public void executeStep(PMachine sender) {
        // pop message from sender queue
        PMessage msg = sender.getSendBuffer().remove();

        isStickyStep = msg.getEvent().isCreateMachineEvent();
        if (!isStickyStep) {
            // update step number
            stepNumber++;
        }

        // reset number of logs in current step
        stepNumLogs = 0;

        // log start step
        PExplicitLogger.logStartStep(stepNumber, sender, msg);

        // process message
        processDequeueEvent(sender, msg);

        // update done stepping flag
        isDoneStepping = (stepNumber >= PExplicitGlobal.getConfig().getMaxStepBound());
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
        int machineCount = stepState.getMachineCount(machineType);
        PMachineId pid = new PMachineId(machineType, machineCount);
        PMachine machine = PExplicitGlobal.getGlobalMachine(pid);
        if (machine == null) {
            // create a new machine
            machine = constructor.apply(machineCount);
            PExplicitGlobal.addGlobalMachine(machine, machineCount);
        }

        // add machine to schedule
        stepState.makeMachine(machine);
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
    public void processDequeueEvent(PMachine sender, PMessage message) {
        if (message.getEvent().isCreateMachineEvent()) {
            // log create machine
            PExplicitLogger.logCreateMachine(message.getTarget(), sender);
        } else {
            // log monitor process event
            PExplicitLogger.logDequeueEvent(message.getTarget(), message);
        }

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
        for (PMachine machine : stepState.getMachineSet()) {
            if (machine.canRun() && machine.isBlocked()) {
                Assert.deadlock(String.format("Deadlock detected. %s is waiting to receive an event, but no other controlled tasks are enabled.", machine));
            }
        }
    }

    /**
     * Check for liveness at the end of a completed schedule
     */
    public void checkLiveness(boolean terminated) {
        for (PMachine monitor : PExplicitGlobal.getModel().getMonitors()) {
            if (monitor.getCurrentState().isHotState()) {
                if (terminated) {
                    Assert.liveness(String.format("Monitor %s detected liveness bug in hot state %s at the end of program execution", monitor, monitor.getCurrentState()));
                } else {
                    Assert.liveness(String.format("Monitor %s detected potential liveness bug in hot state %s", monitor, monitor.getCurrentState()));
                }
            }
        }
    }
}
