package pcover.runtime.scheduler;

import lombok.Getter;
import pcover.runtime.PModel;
import pcover.runtime.logger.TraceLogger;
import pcover.runtime.machine.PMachine;
import pcover.runtime.machine.PMonitor;
import pcover.runtime.machine.events.PMessage;
import pcover.utils.exceptions.NotImplementedException;
import pcover.values.*;

import java.util.*;
import java.util.concurrent.TimeoutException;
import java.util.function.Function;

/**
 * Represents the base class that all schedulers extend.
 */
public abstract class Scheduler implements SchedulerInterface {
    /**
     * P Model
     */
    public final Schedule schedule;
    @Getter
    protected final List<PMachine> machines;
    /**
     * List of all machines along any path
     */
    protected final SortedSet<PMachine> currentMachines;
    /**
     * Whether final result is set or not
     */
    protected final Map<Class<? extends PMachine>, Integer> machineCounters;
    /**
     * Choice depth
     */
    protected final int depth = 0;
    /**
     * Set of machines along the current schedule
     */
    @Getter
    private final PModel model;
    /**
     * The scheduling choices made
     */
    public boolean isFinalResult = false;
    /**
     * How many instances of each Machine there are
     */
    protected boolean done = false;
    /**
     * Whether or not search is done
     */
    protected int choiceDepth = 0;
    /**
     * Current depth of exploration
     */
    protected Boolean stickyStep = true;
    /**
     * Flag whether current step is a create or sync machine step
     */
    protected boolean allMachinesHalted = false;
    /**
     * Flag whether current execution finished
     */
    protected boolean terminalLivenessEnabled = true;
    /**
     * Flag whether check for liveness at the end
     */
    List<PMonitor> monitors;
    /**
     * List of monitors instances
     */
    private PMachine start;
    /**
     * The machine to start with
     */
    private Map<PEvent, List<PMonitor>> listeners;                     /* The map from events to listening monitors */

    /**
     * Constructor
     *
     * @param p        Program
     * @param machines The machines initially in the scheduler
     */
    protected Scheduler(PModel p, PMachine... machines) {
        model = p;
        this.schedule = new Schedule();
        this.machines = new ArrayList<>();
        this.currentMachines = new TreeSet<>();
        this.machineCounters = new HashMap<>();

        for (PMachine machine : machines) {
            this.machines.add(machine);
            this.currentMachines.add(machine);
            if (this.machineCounters.containsKey(machine.getClass())) {
                this.machineCounters.put(machine.getClass(), this.machineCounters.get(machine.getClass()) + 1);
            } else {
                this.machineCounters.put(machine.getClass(), 1);
            }
            TraceLogger.onCreateMachine(machine);
            schedule.makeMachine(machine);
        }
    }

    /**
     * TODO
     */
    public abstract void doSearch() throws TimeoutException, InterruptedException;

    /**
     * TODO
     */
    public abstract void resumeSearch() throws TimeoutException, InterruptedException;

    /**
     * TODO
     */
    protected abstract void performSearch() throws TimeoutException;

    /**
     * TODO
     */
    protected abstract void step() throws TimeoutException;

    /**
     * TODO
     */
    protected abstract PMachine getNextSchedulingChoice();

    /**
     * TODO
     */
    protected abstract PValue<?> getNextDataChoice(List<PValue<?>> choices);

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
        for (int i = 0; i < bound.getValue(); i++) {
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
        return getRandomEntry(map.getKeys().toList());
    }

    /**
     * TODO
     */
    public void startWith(PMachine machine) {
        if (this.machineCounters.containsKey(machine.getClass())) {
            this.machineCounters.put(machine.getClass(), this.machineCounters.get(machine.getClass()) + 1);
        } else {
            this.machineCounters.put(machine.getClass(), 1);
        }

        machines.add(machine);
        currentMachines.add(machine);
        start = machine;
        TraceLogger.onCreateMachine(machine);
        schedule.makeMachine(machine);

        processEventAtTarget(new PMessage(PEvent.createMachine, machine, null));
    }

    /**
     * TODO
     */
    protected void initializeSearch() {
        assert (depth == 0);

        listeners = model.getListeners();
        monitors = new ArrayList<>(model.getMonitors());
        for (PMachine m : model.getMonitors()) {
            startWith(m);
        }
        PMachine target = model.getStart();
        startWith(target);
        start = target;
    }

    /**
     * Remove a message from the send buffer of a machine.
     *
     * @param machine Machine to remove a message from
     * @return Message
     */
    protected PMessage rmBuffer(PMachine machine) {
        return machine.getSendBuffer().remove();
    }

    /**
     * TODO
     *
     * @param count
     * @param constructor
     * @return
     */
    public PMachine setupNewMachine(
            int count,
            Function<Integer, ? extends PMachine> constructor) {
        PMachine newMachine = constructor.apply(count);

        if (!machines.contains(newMachine)) {
            machines.add(newMachine);
        }
        currentMachines.add(newMachine);
        assert (machines.size() >= currentMachines.size());

        TraceLogger.onCreateMachine(newMachine);
        schedule.makeMachine(newMachine);
        return newMachine;
    }

    /**
     * Run all monitors observing this message
     *
     * @param message Message
     */
    public void runMonitors(PMessage message) {
        List<PMonitor> listenersForEvent = listeners.get(message.getEvent());
        if (listenersForEvent != null) {
            for (PMonitor m : listenersForEvent) {
                m.processEventToCompletion(message);
            }
        }
    }

    /**
     * Process the event at the target machine
     *
     * @param message Message to process
     */
    public void processEventAtTarget(PMessage message) {
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
}
