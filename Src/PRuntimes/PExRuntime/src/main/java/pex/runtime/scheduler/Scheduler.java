package pex.runtime.scheduler;

import lombok.Getter;
import lombok.Setter;
import pex.runtime.PExGlobal;
import pex.runtime.logger.SchedulerLogger;
import pex.runtime.machine.PMachine;
import pex.runtime.machine.PMachineId;
import pex.runtime.machine.PMonitor;
import pex.runtime.scheduler.explicit.StepState;
import pex.utils.exceptions.NotImplementedException;
import pex.utils.misc.Assert;
import pex.values.*;

import java.lang.reflect.InvocationTargetException;
import java.util.*;
import java.util.concurrent.TimeoutException;

/**
 * Represents the base class that all schedulers extend.
 */
public abstract class Scheduler implements SchedulerInterface {
    /**
     * Current schedule
     */
    @Getter
    protected final Schedule schedule;
    @Getter
    protected final int schedulerId;
    @Getter
    protected final SchedulerLogger logger;
    /**
     * Mapping from machine type to list of all machine instances
     */
    private final Map<Class<? extends PMachine>, List<PMachine>> machineListByType = new HashMap<>();
    /**
     * Set of machines
     */
    @Getter
    private final SortedSet<PMachine> machineSet = new TreeSet<>();
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
    protected Scheduler(int schedulerId, Schedule sch) {
        this.schedulerId = schedulerId;
        this.schedule = sch;
        this.logger = new SchedulerLogger(schedulerId);
    }

    /**
     * Constructor
     */
    protected Scheduler(int schedulerId) {
        this(schedulerId, new Schedule());
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
        if (stepNumLogs >= PExGlobal.getConfig().getMaxStepLogBound()) {
            Assert.liveness("Detected potential infinite loop in an atomic block");
        }
    }

    /**
     * Get the next random boolean choice
     *
     * @return boolean data choice
     */
    public PBool getRandomBool(String loc) {
        List<PValue<?>> choices = new ArrayList<>();
        stepState.updateChoiceStats(loc, 2);

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
    public PInt getRandomInt(String loc, PInt bound) {
        List<PValue<?>> choices = new ArrayList<>();
        int boundInt = bound.getValue();
        if (boundInt == 0) {
            boundInt = 1;
        }
        stepState.updateChoiceStats(loc, boundInt);

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
    protected PValue<?> getRandomEntry(String loc, List<PValue<?>> choices) {
        PInt randomEntryIdx = getRandomInt(loc, new PInt(choices.size()));
        return choices.get(randomEntryIdx.getValue());
    }

    /**
     * Get the next random element from a PSeq.
     *
     * @param seq PSeq object
     * @return data choice
     */
    public PValue<?> getRandomEntry(String loc, PSeq seq) {
        return getRandomEntry(loc, seq.toList());
    }

    /**
     * Get the next random element from a PSet.
     *
     * @param set PSet object
     * @return data choice
     */
    public PValue<?> getRandomEntry(String loc, PSet set) {
        return getRandomEntry(loc, set.toList());
    }

    /**
     * Get the next random key from a PMap.
     *
     * @param map PMap object
     * @return data choice
     */
    public PValue<?> getRandomEntry(String loc, PMap map) {
        return getRandomEntry(loc, map.toList());
    }

    /**
     * Start the scheduler.
     * Starts monitors and main machine.
     */
    protected void start() {
        // start monitors first
        for (Class<? extends PMachine> monitorType : PExGlobal.getModel().getTestDriver().getMonitors()) {
            startMachine(monitorType);
        }

        // start main machine
        startMachine(PExGlobal.getModel().getTestDriver().getStart());
    }

    /**
     * Start a machine.
     * Runs the constructor of this machine.
     */
    public void startMachine(Class<? extends PMachine> machineType) {
        PMachine machine = allocateMachine(machineType);

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
        logger.logStartStep(stepNumber, sender, msg);

        // process message
        processDequeueEvent(sender, msg);

        // update done stepping flag
        isDoneStepping = (stepNumber >= PExGlobal.getConfig().getMaxStepBound());
    }

    /**
     * Get a machine of a given type and index if exists, else return null.
     *
     * @param pid Machine pid
     * @return Machine
     */
    public PMachine getMachine(PMachineId pid) {
        List<PMachine> machinesOfType = machineListByType.get(pid.getType());
        if (machinesOfType == null) {
            return null;
        }
        if (pid.getTypeId() >= machinesOfType.size()) {
            return null;
        }
        PMachine result = machineListByType.get(pid.getType()).get(pid.getTypeId());
        assert (machineSet.contains(result));
        return result;
    }

    /**
     * Add a machine.
     *
     * @param machine      Machine to add
     * @param machineCount Machine type count
     */
    public void addMachine(PMachine machine, int machineCount) {
        if (!machineListByType.containsKey(machine.getClass())) {
            machineListByType.put(machine.getClass(), new ArrayList<>());
        }
        assert (machineCount == machineListByType.get(machine.getClass()).size());
        machineListByType.get(machine.getClass()).add(machine);
        machineSet.add(machine);
        assert (machineListByType.get(machine.getClass()).get(machineCount) == machine);
    }

    /**
     * Allocate a machine
     *
     * @param machineType
     * @return
     */
    public PMachine allocateMachine(Class<? extends PMachine> machineType) {
        // get machine count for given type from schedule
        int machineCount = stepState.getMachineCount(machineType);
        PMachineId pid = new PMachineId(machineType, machineCount);
        PMachine machine = getMachine(pid);
        if (machine == null) {
            // create a new machine
            try {
                machine = machineType.getDeclaredConstructor(int.class).newInstance(machineCount);
            } catch (InstantiationException | IllegalAccessException | InvocationTargetException |
                     NoSuchMethodException e) {
                throw new RuntimeException(e);
            }
            addMachine(machine, machineCount);
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
        List<Class<? extends PMachine>> listenersForEvent = PExGlobal.getModel().getTestDriver().getListeners().get(message.getEvent());
        if (listenersForEvent != null) {
            for (Class<? extends PMachine> machineType : listenersForEvent) {
                PMonitor m = (PMonitor) getMachine(new PMachineId(machineType, 0));

                // log monitor process event
                logger.logMonitorProcessEvent(m, message);

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
            logger.logCreateMachine(message.getTarget(), sender);
        } else {
            // log monitor process event
            logger.logDequeueEvent(message.getTarget(), message);
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
        for (PMachine machine : stepState.getMachines()) {
            if (machine.canRun() && machine.isBlocked()) {
                Assert.deadlock(String.format("Deadlock detected. %s is waiting to receive an event, but no other controlled tasks are enabled.", machine));
            }
        }
    }

    /**
     * Check for liveness at the end of a completed schedule
     */
    public void checkLiveness(boolean terminated) {
        if (terminated) {
            for (Class<? extends PMachine> machineType : PExGlobal.getModel().getTestDriver().getMonitors()) {
                PMonitor monitor = (PMonitor) getMachine(new PMachineId(machineType, 0));

                if (monitor.getCurrentState().isHotState()) {
                    Assert.liveness(String.format("Monitor %s detected liveness bug in hot state %s at the end of program execution", monitor, monitor.getCurrentState()));
                }
            }
        }
    }
}
