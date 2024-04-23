package pexplicit.runtime.machine;

import lombok.Getter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.machine.buffer.DeferQueue;
import pexplicit.runtime.machine.buffer.SenderQueue;
import pexplicit.runtime.machine.eventhandlers.EventHandler;
import pexplicit.runtime.machine.events.PContinuation;
import pexplicit.utils.exceptions.BugFoundException;
import pexplicit.utils.misc.Assert;
import pexplicit.utils.serialize.SerializableBiFunction;
import pexplicit.utils.serialize.SerializableRunnable;
import pexplicit.values.PEvent;
import pexplicit.values.PMachineValue;
import pexplicit.values.PMessage;
import pexplicit.values.PValue;

import java.io.Serializable;
import java.util.*;
import java.util.function.Function;

/**
 * Represents the base class for all P machines.
 */
public abstract class PMachine implements Serializable, Comparable<PMachine> {
    @Getter
    private static final Map<String, PMachine> nameToMachine = new HashMap<>();
    protected static int globalMachineId = 1;
    @Getter
    protected final int typeId;
    @Getter
    protected final String name;
    private final Set<State> states;
    private final State startState;
    @Getter
    private final SenderQueue sendBuffer;
    private final DeferQueue deferQueue;
    @Getter
    private final Map<String, PContinuation> continuationMap = new TreeMap<>();
    /**
     * Unique identifier across all PMachines/PMonitors
     * For PMachines, instanceId runs from 1 to #PMachines
     * For PMonitors, instanceId runs from -1 to -#PMonitors
     */
    @Getter
    protected int instanceId;
    @Getter
    private State currentState;
    @Getter
    private boolean started = false;
    @Getter
    private boolean halted = false;
    private PContinuation blockedBy = null;
    @Getter
    private State blockedStateExit;
    @Getter
    private State blockedNewStateEntry;
    @Getter
    private PValue<?> blockedNewStateEntryPayload;

    /**
     * TODO
     * Machine constructor
     *
     * @param name       Name of the machine
     * @param id         Input id
     * @param startState Start state
     * @param states     All states corresponding to this machine
     */
    public PMachine(String name, int id, State startState, State... states) {
        // initialize name, ids
        this.name = name;
        this.instanceId = ++globalMachineId;
        this.typeId = id;
        nameToMachine.put(toString(), this);

        // initialize states
        this.states = new HashSet<>();
        Collections.addAll(this.states, states);
        this.startState = startState;
        this.currentState = startState;

        // register create machine handler
        startState.registerHandlers(
                new EventHandler(PEvent.createMachine) {
                    @Override
                    public void handleEvent(PMachine target, PValue<?> payload) {
                        assert (!target.isStarted());
                        target.start(payload);
                    }
                });

        // initialize send buffer
        this.sendBuffer = new SenderQueue(this);
        this.deferQueue = new DeferQueue(this);
    }

    public void start(PValue<?> payload) {
        assert (currentState == startState);
        started = true;
        enterNewState(startState, payload);
    }

    public void halt() {
        halted = true;
    }

    /**
     * Check if this machine can run.
     *
     * @return true if machine has started and is not halted, else false
     */
    public boolean canRun() {
        return started && !halted;
    }

    /**
     * Reset the machine.
     */
    public void reset() {
        this.currentState = startState;

        this.sendBuffer.clear();
        this.deferQueue.clear();

        this.started = false;
        this.halted = false;

        this.blockedBy = null;
        this.blockedStateExit = null;
        this.blockedNewStateEntry = null;
        this.blockedNewStateEntryPayload = null;
    }

    /**
     * Get names of local variables as an ordered list
     *
     * @return List of strings
     */
    public List<String> getLocalVarNames() {
        List<String> result = new ArrayList<>();

        result.add("_currentState");

        result.add("_sendBuffer");
        result.add("_deferQueue");

        result.add("_started");
        result.add("_halted");

        result.add("_blockedBy");
        result.add("_blockedStateExit");
        result.add("_blockedNewStateEntry");
        result.add("_blockedNewStateEntryPayload");

        return result;
    }

    /**
     * Get values of local variables as an ordered list
     *
     * @return List of values
     */
    public List<Object> getLocalVarValues() {
        List<Object> result = new ArrayList<>();

        result.add(currentState);

        result.add(sendBuffer.getElements());
        result.add(deferQueue.getElements());

        result.add(started);
        result.add(halted);

        result.add(blockedBy);
        result.add(blockedStateExit);
        result.add(blockedNewStateEntry);
        result.add(blockedNewStateEntryPayload);

        return result;
    }

    /**
     * Copy values of local variables as an ordered list
     *
     * @return List of values
     */
    public List<Object> copyLocalVarValues() {
        List<Object> result = new ArrayList<>();

        result.add(currentState);

        result.add(new ArrayList<>(sendBuffer.getElements()));
        result.add(new ArrayList<>(deferQueue.getElements()));

        result.add(started);
        result.add(halted);

        result.add(blockedBy);
        result.add(blockedStateExit);
        result.add(blockedNewStateEntry);
        result.add(blockedNewStateEntryPayload);

        return result;
    }

    /**
     * Set local variables
     *
     * @param values Ordered list of values to set to
     * @return Next index in the list of values
     */
    protected int setLocalVarValues(List<Object> values) {
        int idx = 0;

        currentState = (State) values.get(idx++);

        sendBuffer.setElements((List<PMessage>) values.get(idx++));
        deferQueue.setElements((List<PMessage>) values.get(idx++));

        started = (boolean) values.get(idx++);
        halted = (boolean) values.get(idx++);

        blockedBy = (PContinuation) values.get(idx++);
        blockedStateExit = (State) values.get(idx++);
        blockedNewStateEntry = (State) values.get(idx++);
        blockedNewStateEntryPayload = (PValue<?>) values.get(idx++);

        return idx;
    }

    public MachineLocalState copyMachineState() {
        return new MachineLocalState(copyLocalVarValues());
    }

    public void setMachineState(MachineLocalState input) {
        setLocalVarValues(input.getLocals());
    }

    /**
     * Create a new machine instance
     *
     * @param machineType Machine type
     * @param payload     payload associated with machine's constructor
     * @param constructor Machine constructor
     * @return New machine as a PMachineValue
     */
    public PMachineValue create(
            Class<? extends PMachine> machineType,
            PValue<?> payload,
            Function<Integer, ? extends PMachine> constructor) {
        PMachine machine = PExplicitGlobal.getScheduler().allocateMachine(machineType, constructor);
        PMessage msg = new PMessage(PEvent.createMachine, machine, payload);
        sendBuffer.add(msg);
        return new PMachineValue(machine);
    }

    /**
     * Create a new machine instance
     *
     * @param machineType Machine type
     * @param constructor Machine constructor
     * @return New machine as a PMachineValue
     */
    public PMachineValue create(
            Class<? extends PMachine> machineType,
            Function<Integer, ? extends PMachine> constructor) {
        return create(machineType, null, constructor);
    }

    /**
     * Send an event to a target machine
     *
     * @param target  Target machine
     * @param event   PEvent to send
     * @param payload Payload corresponding to the event
     */
    public void sendEvent(PMachineValue target, PEvent event, PValue<?> payload) {
        if (PValue.isEqual(target, null)) {
            throw new BugFoundException("Machine in send event cannot be null.");
        }

        PMessage msg = new PMessage(event, target.getValue(), payload);

        // log send event
        PExplicitLogger.logSendEvent(this, msg);

        sendBuffer.add(msg);
        PExplicitGlobal.getScheduler().runMonitors(msg);
    }

    /**
     * Goto a state
     *
     * @param state   State to go to
     * @param payload Payload for entry function of the state
     */
    public void gotoState(State state, PValue<?> payload) {
        processStateTransition(state, payload);
    }

    /**
     * Register a continuation
     *
     * @param name      Name of the continuation
     * @param handleFun Function executed when unblocking
     * @param clearFun  Function that clears corresponding continuation variables
     */
    protected void registerContinuation(
            String name,
            SerializableBiFunction<PMachine, PMessage> handleFun,
            SerializableRunnable clearFun,
            String... caseEvents) {
        continuationMap.put(name, new PContinuation(handleFun, clearFun, caseEvents));
    }

    /**
     * Block at a continuation
     *
     * @param continuationName Continuation name
     */
    public void blockUntil(String continuationName) {
        blockedBy = continuationMap.get(continuationName);

        // log receive
        PExplicitLogger.logReceive(this, blockedBy);
    }

    public boolean isBlocked() {
        return blockedBy != null;
    }

    public void clearBlocked() {
        blockedBy = null;
    }

    public boolean isDeferred(PEvent event) {
        if (currentState.isDeferred(event)) {
            return true;
        }
        if (isBlocked()) {
            return blockedBy.isDeferred(event);
        }
        return false;
    }

    /**
     * Process an event until completion
     *
     * @param msg Message
     */
    public void processEventToCompletion(PMessage msg) {
        // run msg to completion

        // do nothing if already halted
        if (isHalted()) {
            return;
        }

        // do nothing if event is ignored in current state
        if (currentState.isIgnored(msg.getEvent())) {
            return;
        }

        runDeferredEvents();

        // process the event
        processEvent(msg);

        runDeferredEvents();
    }

    /**
     * Run events from the deferred queue
     */
    void runDeferredEvents() {
        // do nothing if already halted
        if (isHalted()) {
            return;
        }

        List<PMessage> deferredMessages = new ArrayList<>(deferQueue.getElements());
        deferQueue.clear();
        for (PMessage msg : deferredMessages) {
            processEvent(msg);
        }
    }

    /**
     * Process an event at the current state.
     *
     * @param message Message to process
     */
    void processEvent(PMessage message) {
        if (isDeferred(message.getEvent())) {
            deferQueue.add(message);
            return;
        }

        runEvent(message);
    }

    /**
     * Run an event at the current state.
     *
     * @param message Message to process
     */
    void runEvent(PMessage message) {
        if (isBlocked()) {
            PContinuation currBlockedBy = this.blockedBy;
            PEvent event = message.getEvent();
            clearBlocked();

            // make sure event is handled (or is halt event)
            if (currBlockedBy.getCaseEvents().contains(event.toString())) {
                currBlockedBy.getHandleFun().apply(this, message);

                // post process
                currBlockedBy.runAfter(this);
            } else if (event.isHaltMachineEvent()) {
                this.halt();
            } else {
                Assert.fromModel(false,
                        String.format("Unexpected event %s received in a receive for machine %s in state %s",
                                event, this, this.currentState));
            }
        } else {
            currentState.handleEvent(message, this);
        }
    }

    /**
     * Raise an event
     *
     * @param event   Event to raise
     * @param payload Payload
     */
    public void raiseEvent(PEvent event, PValue<?> payload) {
        // do nothing if already halted
        if (isHalted()) {
            return;
        }

        // do nothing if event is ignored in current state
        if (currentState.isIgnored(event)) {
            return;
        }

        PMessage msg = new PMessage(event, this, payload);

        // log raise event
        PExplicitLogger.logRaiseEvent(this, event);

        // run the event (even if deferred)
        runEvent(msg);

        runDeferredEvents();
    }

    /**
     * Raise an event
     *
     * @param event Event to raise
     */
    public void raiseEvent(PEvent event) {
        raiseEvent(event, null);
    }

    /**
     * Process state transition to a new state
     *
     * @param newState New state to transition to
     * @param payload  Entry function payload for the new state
     */
    public void processStateTransition(State newState, PValue<?> payload) {
        if (isBlocked()) {
            blockedStateExit = currentState;
            blockedNewStateEntry = newState;
            blockedNewStateEntryPayload = payload;
            return;
        }

        // log state transition
        PExplicitLogger.logStateTransition(this, newState);

        if (currentState != null) {
            // execute exit function of current state
            exitCurrentState();

            if (isBlocked()) {
                blockedNewStateEntry = newState;
                blockedNewStateEntryPayload = payload;
                return;
            }
        }

        // enter the new state
        enterNewState(newState, payload);
    }

    public void exitCurrentState() {
        // do nothing if already halted
        if (isHalted()) {
            return;
        }

        blockedStateExit = null;

        PExplicitLogger.logStateExit(this);
        currentState.exit(this);
    }

    public void enterNewState(State newState, PValue<?> payload) {
        // do nothing if already halted
        if (isHalted()) {
            return;
        }

        blockedNewStateEntry = null;
        blockedNewStateEntryPayload = null;

        // change current state to new state
        currentState = newState;

        PExplicitLogger.logStateEntry(this);

        // change current state to new state
        newState.entry(this, payload);
    }

    @Override
    public int compareTo(PMachine rhs) {
        if (rhs == null) {
            return this.instanceId;
        }
        return (this.instanceId - rhs.instanceId);
    }

    @Override
    public int hashCode() {
        return this.instanceId;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof PMachine)) {
            return false;
        }
        if (this.name == null) {
            return (((PMachine) obj).name == null);
        }
        return this.name.equals(((PMachine) obj).name)
                && this.instanceId == (((PMachine) obj).instanceId);
    }

    @Override
    public String toString() {
        return String.format("%s(%d)", name, instanceId);
    }
}