package prt.exceptions;

import prt.PState;

import java.util.Objects;
import java.util.Optional;

/**
 * A prt.exceptions.GotoTransitionException is raised by user handlers when they would like to transition
 * to a new state.
 *
 * Internal note: Java doesn't let us override j.l.Throwable with a parameterized subtype,
 * which I didn't know until just now!  For the moment, we are doing some unchecked casts
 * to get around the fact that we can't specify the StateKey type.  I wonder if that bit
 * of polymorphism is more trouble than it's worth, and we should simply use String keys.
 */
public class GotoTransitionException extends Exception {
    private PState targetState;
    private Optional<Object> payload;

    public PState getTargetState() {
        return targetState;
    }

    public Optional<Object> getPayload() {
        return payload;
    }

    public GotoTransitionException(PState s) {
        Objects.requireNonNull(s);

        this.targetState = s;
        this.payload = Optional.empty();
    }

    public GotoTransitionException(PState s, Object payload) {
        Objects.requireNonNull(s);
        Objects.requireNonNull(payload);

        this.targetState = s;
        this.payload = Optional.of(payload);
    }
}
