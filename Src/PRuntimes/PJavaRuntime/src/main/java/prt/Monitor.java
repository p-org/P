package prt;

import java.util.HashMap;
import java.util.Objects;
import java.util.Optional;

import prt.events.PEvent;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.Marker;
import org.apache.logging.log4j.MarkerManager;
import org.apache.logging.log4j.message.StringMapMessage;

/**
 * A prt.Monitor encapsulates a state machine.
 *
 */
public class Monitor {
    private final Logger logger = LogManager.getLogger(this.getClass());
    private static final Marker PROCESSING_MARKER = MarkerManager.getMarker("EVENT_PROCESSING");
    private static final Marker TRANSITIONING_MARKER = MarkerManager.getMarker("STATE_TRANSITIONING");

    @SuppressWarnings("OptionalUsedAsFieldOrParameterType")
    private Optional<State> startState;
    private State currentState;

    private final HashMap<String, State> states;

    /**
     * If the prt.Monitor is running, new states must not be able to be added.
     * If the monitor is not running, events must not be able to be processed and states can't be transitioned.
     */
    private boolean isRunning;

    /**
     * Adds a new prt.State to the state machine.
     *
     * @param s The state.
     */
    protected void addState(State s) {
        Objects.requireNonNull(s);
        if (isRunning) {
            throw new RuntimeException("prt.Monitor is already running; no new states may be added.");
        }

        if (states.containsKey(s.getKey())) {
            throw new RuntimeException("prt.State already present");
        }
        states.put(s.getKey(), s);

        if (s.isInitialState()) {
            if (startState.isPresent()) {
                throw new RuntimeException("Initial state already set to " + startState.get().getKey());
            }
            startState = Optional.of(s);
        }
    }

    public String getCurrentState() {
        return currentState.getKey();
    }

    /**
     * Throws a runtime exception if the given boolean is false.
     * @param cond The predicate to assert on.
     * @param msg The message to deliver if the predicate is false.
     */
    protected void tryAssert(boolean cond, String msg)
    {
        if (!cond) throw new PAssertionFailureException(msg);
    }

    /**
     * Interrupts the current event handler and processes the given event in the current state
     * @param ev The event to process.
     * @throws RaiseEventException to context-switch back into the runtime.
     */
    @SuppressWarnings(value = "unchecked")
    protected <P> void tryRaiseEvent(PEvent<P> ev) throws RaiseEventException
    {
        throw new RaiseEventException((PEvent<Object>) ev);
    }

    /**
     * Transitions the prt.Monitor to a new state, without including a payload.
     *
     * @param k the key of the state to transition to.
     *
     * @throws RuntimeException if `k` is not a state in the state machine.
     */
    protected void gotoState(String k) throws TransitionException {
        Objects.requireNonNull(k);

        if (!states.containsKey(k)) {
            throw new RuntimeException("prt.State not present");
        }
        throw new TransitionException(states.get(k));
    }

    /**
     * Transitions the prt.Monitor to a new state, delivering the given event afterwards.
     *
     * @param k the key of the state to transition to.
     * @param payload The payload to hand to the state entry handler.
     *
     * @throws RuntimeException if `k` is not a state in the state machine.
     */
    protected <P> void gotoState(String k, P payload) throws TransitionException {
        Objects.requireNonNull(k);
        Objects.requireNonNull(payload);

        if (!states.containsKey(k)) {
            throw new RuntimeException("prt.State not present");
        }
        throw new TransitionException(states.get(k), payload);
    }

    /**
     * Synchronously processes one Event.Payload message.
     *
     * @param p the pEvent.
     * @throws UnhandledEventException if the pEvent's type has no associated handler.
     */
    @SuppressWarnings(value = "unchecked")
    public <P> void process(PEvent p) throws UnhandledEventException {
        Objects.requireNonNull(p);

        if (!isRunning) {
            throw new RuntimeException("prt.Monitor is not running (did you call ready()?)");
        }

        logger.info(PROCESSING_MARKER, new StringMapMessage().with("event", p));

        Optional<State.TransitionableConsumer<P>> oc = currentState.getHandler(p.getClass());
        if (oc.isEmpty()) {
            logger.atFatal().log(currentState + " missing event handler for " + p.getClass().getSimpleName());
            throw new UnhandledEventException(currentState, p.getClass());
        }

        invokeWithTrampoline(oc.get(), (P) p.getPayload());
    }

    /**
     * Transitions to `s` by invoking the current state's exit handler and the new state's
     * entry handler, and updating internal bookkeeping.
     * @param s The new state.
     */
    private <P> void handleTransition(State s, Optional<P> payload) {
        if (!isRunning) {
            throw new RuntimeException("prt.Monitor is not running (did you call ready()?)");
        }

        logger.info(TRANSITIONING_MARKER, new StringMapMessage().with("state", s));

        currentState.getOnExit().ifPresent(Runnable::run);
        currentState = s;

        currentState.getOnEntry().ifPresent(handler -> {
            Object p = payload.orElse(null);
            invokeWithTrampoline(handler, p);
        });
    }

    /**
     * Invokes a given Consumer, handling all its checked exceptions.
     * @param handler The TransitionableConsumer to be invoked.
     * @param o The argument to handler.
     * @param <P> The type to be consumed by the handler.
     */
    private <P> void invokeWithTrampoline(State.TransitionableConsumer<P> handler, P o)
    {
        try {
            // Run the event handler, knowing that it might cause:
            handler.accept(o);
        } catch (TransitionException e) {
            // ...A state transition: if it does, run the exit handler, context-switch, and run
            // the new state's entry handler.
            handleTransition(e.getTargetState(), e.getPayload());
        } catch (RaiseEventException e) {
            // ...An event to be raised.  If it does, process the event in the current state.
            process(e.getEvent());
        } catch (ClassCastException e) {
            // ...An invalid cast: in the case where the event handler's type parameter is incompatible
            // with the runtime type of `p`.  This is a code generation or programming error.
            throw new GotoPayloadClassException(o, currentState);
        }
    }

    /**
     * Marks the prt.Monitor as ready to run and consume events.  The initial state's entry handler, which
     * must be a handler of zero parameters, will be invoked.
     */
    public void ready() {
        readyImpl(Optional.empty());
    }

    /**
     * Marks the Monitor as ready to run and consume events.  The initial state's entry handler, which must
     * be a handler that consumes a payload of type P, will be invoked with the given argument.
     * @param payload The argument to the initial state's entry handler.
     */
    public <P> void ready(P payload) {
        readyImpl(Optional.of(payload));
    }

    private <P> void readyImpl(Optional<P> payload) {
        if (isRunning)
            throw new RuntimeException("prt.Monitor is already running.");

        isRunning = true;

        State s = startState.orElseThrow(() ->
                new RuntimeException("No initial state set (did you specify an initial state, or is the machine halted?)"));
        handleTransition(s, payload);
    }

    /**
     * Instantiates a new prt.Monitor; users should provide domain-specific functionality in a subclass.
     */
    protected Monitor() {
        startState = Optional.empty();
        states = new HashMap<>();
        isRunning = false;

        currentState = new State.Builder("_pre_init").build();
    }
}
