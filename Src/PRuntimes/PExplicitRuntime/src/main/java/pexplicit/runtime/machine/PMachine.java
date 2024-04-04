package pexplicit.runtime.machine;

import lombok.Getter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.machine.buffer.FifoQueue;
import pexplicit.runtime.machine.eventhandlers.EventHandler;
import pexplicit.runtime.machine.events.PContinuation;
import pexplicit.runtime.machine.events.PMessage;
import pexplicit.utils.exceptions.NotImplementedException;
import pexplicit.utils.serialize.SerializableBiFunction;
import pexplicit.utils.serialize.SerializableRunnable;
import pexplicit.values.PEvent;
import pexplicit.values.PMachineValue;
import pexplicit.values.PValue;

import java.io.Serializable;
import java.util.*;
import java.util.function.Function;

/**
 * Represents the base class for all P machines.
 */
public abstract class PMachine implements Serializable, Comparable<PMachine> {
    @Getter
    private static final int mainMachineId = 2;
    @Getter
    private static final Map<String, PMachine> nameToMachine = new HashMap<>();
    protected static int globalMachineId = mainMachineId;

    @Getter
    protected int instanceId;
    @Getter
    protected final int typeId;
    @Getter
    protected final String name;

    private final Set<State> states;
    private final State startState;
    @Getter
    private State currentState;

    @Getter
    private final FifoQueue sendBuffer;

    @Getter
    private boolean started = false;
    @Getter
    private boolean halted = false;

    private final Map<String, PContinuation> continuationMap = new TreeMap<>();

    private PContinuation blockedBy = null;

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
        this.instanceId = globalMachineId++;
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
        this.sendBuffer = new FifoQueue(this);
    }

    public void start(PValue<?> payload) {
        PExplicitLogger.logMachineStart(this);
        assert (currentState == startState);
        started = true;

        startState.entry(this, payload);
    }

    public void halt() {
        halted = true;
    }

    /**
     * Check if this machine can run.
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
        this.started = false;
        this.halted = false;
    }

    /**
     * TODO
     *
     * @return
     */
    protected List<Object> getLocalVars() {
        throw new NotImplementedException();
    }

    /**
     * TODO
     *
     * @param localVars
     * @return
     */
    protected int setLocalVars(List<Object> localVars) {
        throw new NotImplementedException();
    }

    /**
     * Create a new machine instance
     *
     * @param machineType Machine type
     * @param payload payload associated with machine's constructor
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
     * TODO
     *
     * @param machineType
     * @param constructor
     * @return
     */
    public PMachineValue create(
            Class<? extends PMachine> machineType,
            Function<Integer, ? extends PMachine> constructor) {
        return create(machineType, null, constructor);
    }

    /**
     * Send an event to a target machine
     *
     * @param target Target machine
     * @param event PEvent to send
     * @param payload Payload corresponding to the event
     */
    public void sendEvent(PMachineValue target, PEvent event, PValue<?> payload) {
        PMessage msg = new PMessage(event, target.getValue(), payload);
        sendBuffer.add(msg);
    }

    /**
     * Goto a state
     *
     * @param state State to go to
     * @param payload Payload for entry function of the state
     */
    public void gotoState(State state, PValue<?> payload) {
        processStateTransition(state, payload);
    }

    /**
     * Register a continuation
     * @param name Name of the continuation
     * @param handleFun Function executed when unblocking
     * @param clearFun Function that clears corresponding continuation variables
     */
    protected void registerContinuation(
            String name,
            SerializableBiFunction<PMachine, PMessage> handleFun,
            SerializableRunnable clearFun,
            String ... caseEvents) {
        continuationMap.put(name, new PContinuation(handleFun, clearFun, caseEvents));
    }

    /**
     * TODO
     * @param continuationName
     */
    public void blockUntil(String continuationName) {
        blockedBy = continuationMap.get(continuationName);
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

        // make sure event is not deferred in current state
        assert (!isDeferred(msg.getEvent()));

        // process the event
        processEvent(msg);
    }

    /**
     * Process an event at the current state.
     * @param message Message to process
     */
    void processEvent(PMessage message) {
        PExplicitLogger.logEvent(message);
        if (isBlocked()) {
            PContinuation currBlockedBy = this.blockedBy;
            clearBlocked();
            currBlockedBy.getHandleFun().apply(this, message);
            if (!isBlocked()) {
                for (PContinuation c : continuationMap.values()) {
                    c.getClearFun().run();
                }
            }
        } else {
            currentState.handleEvent(message, this);
        }
    }

    /**
     * Process state transition to a new state
     * @param newState New state to transition to
     * @param payload Entry function payload for the new state
     */
    public void processStateTransition(State newState, PValue<?> payload) {
        PExplicitLogger.logStateTransition(this, newState);

        if (currentState != null) {
            // execute exit function of current state
            currentState.exit(this);
        }

        // change current state to new state
        currentState = newState;

        // change current state to new state
        newState.entry(this, payload);
    }

    @Override
    public int compareTo(PMachine rhs) {
        return instanceId - rhs.getInstanceId();
    }

    @Override
    public int hashCode() {
        if (name == null) return instanceId;
        return name.hashCode() ^ instanceId;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof PMachine)) {
            return false;
        }
        if (this.name == null)
            return (((PMachine) obj).name == null) && this.instanceId == (((PMachine) obj).instanceId);
        return this.name.equals(((PMachine) obj).name)
                && this.instanceId == (((PMachine) obj).instanceId);
    }

    @Override
    public String toString() {
        return String.format("%s(%d)", name, instanceId);
    }
}