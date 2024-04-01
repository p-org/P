package pexplicit.runtime.machine;

import lombok.Getter;
import pexplicit.runtime.machine.buffer.DeferQueue;
import pexplicit.runtime.machine.buffer.FifoQueue;
import pexplicit.runtime.machine.events.PMessage;
import pexplicit.utils.exceptions.NotImplementedException;
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
    protected final String name;
    private final Set<State> states;
    private final State startState;
    @Getter
    private final State currentState;
    @Getter
    private final FifoQueue sendBuffer;
    @Getter
    private final DeferQueue deferredQueue;
    @Getter
    private final boolean started = false;
    @Getter
    private final boolean halted = false;
    @Getter
    protected int instanceId;


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
        this.name = name;
//        this.instanceId = id;
        this.instanceId = globalMachineId++;
        nameToMachine.put(toString(), this);

        this.startState = startState;
        this.states = new HashSet<>();
        Collections.addAll(this.states, states);

        throw new NotImplementedException();
    }

    /**
     * Check if this machine can run.
     * @return true if machine has started and is not halted, else false
     */
    public boolean canRun() {
        return started && !halted;
    }

    /**
     * TODO
     */
    protected void reset() {
        throw new NotImplementedException();
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
     * TODO
     *
     * @param machineType
     * @param payload
     * @param constructor
     * @return
     */
    public PMachineValue create(
            Class<? extends PMachine> machineType,
            PValue<?> payload,
            Function<Integer, ? extends PMachine> constructor) {
        throw new NotImplementedException();
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
     * TODO
     *
     * @param target
     * @param event
     * @param payload
     */
    public void sendEvent(PMachineValue target, PEvent event, PValue<?> payload) {
        throw new NotImplementedException();
    }

    /**
     * TODO
     *
     * @param state
     * @param payload
     */
    public void gotoState(State state, PValue<?> payload) {
        throw new NotImplementedException();
    }

    /**
     * TODO
     *
     * @param event
     */
    public void unblock(PMessage event) {
        throw new NotImplementedException();
    }

    /**
     * Process an event until completion
     *
     * @param msg Message
     */
    public void processEventToCompletion(PMessage msg) {
        // Process events from the deferred queue first
        runDeferredEvents();

        // Process event
        runOutcomesToCompletion(msg);

        // Process events from the deferred queue again
        runDeferredEvents();
    }

    /**
     * Run events from the deferred queue
     */
    void runDeferredEvents() {
        for (PMessage msg : deferredQueue.getElements()) {
            runOutcomesToCompletion(msg);
        }
    }

    /**
     * TODO
     *
     * @param msg
     */
    void runOutcomesToCompletion(PMessage msg) {
        throw new NotImplementedException();
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