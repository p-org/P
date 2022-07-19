package prt;

import prt.events.PEvent;
import prt.exceptions.RaiseEventException;
import prt.exceptions.TransitionException;

import java.util.HashMap;
import java.util.Objects;
import java.util.Optional;

/**
 * A state in a prt.Monitor's transition diagram.  A state contains zero or more Event handlers; when the prt.Monitor
 * receives an event, it defers behaviour to the current state's handler for that event, if it exists.  (If
 * no handler exists for that particular state, the Event is simply dropped.)
 *
 * To construct a prt.State, use the `prt.State.Builder` interface.
 */
public class State<K extends Enum<K>> {

    public enum Temperature {
        HOT, COLD, UNSET
    }

    /**
     * Functionally-equivalent to a Consumer<T>, but may throw exceptional control flow within accept().
     * @param <T> The type to be consumed.
     */
    @FunctionalInterface
    public interface TransitionableConsumer<T> {
        /**
         * Invokes the consumer with some `t`; PRT runtime control flow exceptions may be thrown prior to the
         * consumer terminating, which the runtime needs to handle.
         * @param t the argument to the function.
         * @throws TransitionException if invoking the function results in a state transition.
         * @throws RaiseEventException if invoking the function results in a raised event.
         */
        void accept(T t) throws TransitionException, RaiseEventException;
    }

    /**
     * Functionally-equivalent to a Runnable, but may throw the checked prt.exceptions.TransitionException within run().
     */
    @FunctionalInterface
    public interface TransitionableRunnable {
        /**
         * Runs the Runnable; a `prt.exceptions.TransitionException` may be thrown prior to the consumer terminating,
         * @throws TransitionException if invoking the function results in a state transition.
         */
        void run() throws TransitionException;
    }

    private final boolean isInitialState;
    private final K key;
    private final Temperature temp;
    private final HashMap<Class<? extends PEvent<?>>, TransitionableConsumer<?>> dispatch;

    @SuppressWarnings("OptionalUsedAsFieldOrParameterType")
    private final Optional<TransitionableConsumer<Object>> onEntry;
    @SuppressWarnings("OptionalUsedAsFieldOrParameterType")
    private final Optional<Runnable> onExit;

    private State(
            HashMap<Class<? extends PEvent<?>>, TransitionableConsumer<?>> dispatch,
            boolean isInitialState,
            K key,
            Optional<TransitionableConsumer<Object>> onEntry,
            Optional<Runnable> onExit,
            Temperature temp) {
        this.dispatch = dispatch;
        this.isInitialState = isInitialState;
        this.key = key;
        this.onEntry = onEntry;
        this.onExit = onExit;
        this.temp = temp;
    }

    /**
     * Returns the (uniquely-) identifying key for this prt.State, used by the prt.Monitor on state transitions.
     *
     * @return the key
     */
    public K getKey() { return key; }

    /**
     * @return The entry handler for this prt.State.
     */
    public Optional<TransitionableConsumer<Object>> getOnEntry() {
        return onEntry;
    }

    /**
     * @return The void-consuming exit handler for this prt.State.
     */
    public Optional<Runnable> getOnExit() {
        return onExit;
    }

    /**
     * Returns whether this prt.State was marked to be the (unique) initial state of its prt.Monitor.
     *
     * @return the boolean
     */
    public boolean isInitialState() { return isInitialState; }

    @Override
    public String toString() {
        return String.format("prt.State[%s]", key);
    }

    /**
     * Returns the handler for a Payload of some given class.
     *
     * @param <P>   the subclass of `Event.Payload` whose handler we're looking up.
     * @param clazz the Java Class whose handler we're looking up.
     * @return the handler that a `P` can be called with.
     */
    @SuppressWarnings(value = "unchecked")
    public <P, PE extends PEvent<P>> Optional<TransitionableConsumer<P>> getHandler(Class<PE> clazz) {
        if (!dispatch.containsKey(clazz)) {
            return Optional.empty();
        }
        TransitionableConsumer<P> handler = (TransitionableConsumer<P>) dispatch.get(clazz);
        return Optional.of(handler);
    }

    /**
     * A factory method to construct a new Builder.  A more fluid interface to the prt.Builder constructor.
     * @param k the key for this eventual state.
     * @return a builder for the state.
     */
    public static <K extends Enum<K>> Builder<K> keyedOn(K k) {
        return new Builder<>(k);
    }

    /**
     * Builds a prt.State.
     */
    static public class Builder<K extends Enum<K>> {
        private boolean isInitialState;

        private final K key;
        private Temperature temp;
        private final HashMap<Class<? extends PEvent<?>>, TransitionableConsumer<?>> dispatch;


        @SuppressWarnings("OptionalUsedAsFieldOrParameterType")
        private Optional<TransitionableConsumer<Object>> onEntry;
        @SuppressWarnings("OptionalUsedAsFieldOrParameterType")
        private Optional<Runnable> onExit;


        /**
         * Instantiates a new Builder.
         *
         * @param _key the uniquely-identifying key for our new prt.State.
         */
        public Builder(K _key) {
            key = _key;
            isInitialState = false;
            dispatch = new HashMap<>();
            onEntry = Optional.empty();
            onExit = Optional.empty();
            temp = Temperature.UNSET;
        }

        /**
         * Sets whether our new prt.State should be the prt.Monitor's initial state.
         */
        public Builder<K> isInitialState(boolean b) {
            isInitialState = b;
            return this;
        }

        /**
         * For a given `class P extends Event.Payload`, register a function `P -> void`
         * to be invoked when the prt.Monitor is currently in this state and receives an Event
         * with Payload type `P`.
         *
         * @param <P>   the subclass of Payload
         * @param clazz the subclass of Payload
         * @param f     the handler to be invoked at runtime.
         */
        public <P, PE extends PEvent<P>> Builder<K> withEvent(Class<PE> clazz, TransitionableConsumer<P> f) {
            Objects.requireNonNull(f);
            Objects.requireNonNull(clazz);

            if (dispatch.containsKey(clazz)) {
                throw new RuntimeException(String.format("Builder already supplied handler for Event %s", clazz.getName()));
            }
            dispatch.put(clazz, f);
            return this;
        }

        /**
         * Sets the temperature for the current state.
         *
         * @param t the temperature.
         */
        public Builder<K> withTemperature(Temperature t) {
            this.temp = t;
            return this;
        }

        /**
         * Register a function `P -> void` to be invoked when the prt.Monitor is currently in
         * another state and transitions to this one with some particular payload.
         *
         * Note: Payloads are untyped, and so conceivably the programmer may configure their
         * state machine to transition to the current state with a payload _other_ than P!
         * In that case, a ClassCastException will be thrown _at runtime_.
         *
         * TODO: we could simply hard-code this consumer to consume an Optional<j.l.Object>?
         * I like that less but makes the "untyped-ness" clearer to developers...
         *
         * @param f the P -> void function to invoke.
         * @return The builder
         * @param <P> The type parameter of the payload we wish to consume.
         */
        @SuppressWarnings(value = "unchecked")
        public <P> Builder<K> withEntry(TransitionableConsumer<P> f) {
            Objects.requireNonNull(f);

            if (onEntry.isPresent()) {
                throw new RuntimeException(String.format("onEntry handler already handled for state %s",key));
            }
            onEntry = Optional.of( o -> { Objects.requireNonNull(o); f.accept((P)o); });
            return this;
        }

        /**
         * Register a function `void -> void` to be invoked when the monitor is currently
         * in another state and transitions to this one.  If a payload was sent along with
         * the transition, it is discarded.
         *
         * @param f the void procedure to invoke.
         * @return the builder
         */
        public Builder<K> withEntry(TransitionableRunnable f) {
            Objects.requireNonNull(f);

            if (onEntry.isPresent()) {
                throw new RuntimeException(String.format("onEntry handler already handled for state %s",key));
            }
            onEntry = Optional.of(__ -> f.run());
            return this;
        }

        public Builder<K> withExit(Runnable f) {
            Objects.requireNonNull(f);

            if (onExit.isPresent()) {
                throw new RuntimeException(String.format("onExit handler already handled for state %s",key));
            }
            onExit = Optional.of(f);
            return this;
        }


        /**
         * Builds the new prt.State.
         *
         * @return the new prt.State
         */
        public State<K> build() {
            return new State(
                    dispatch,
                    isInitialState,
                    key,
                    onEntry,
                    onExit,
                    temp
            );
        }
    }
}
